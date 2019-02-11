using System;
using kCura.EventHandler;
using System.Data.SqlClient;
using DTOs = kCura.Relativity.Client.DTOs;
using kCura.Relativity.Client;
using System.Linq;
using System.Collections.Generic;
using Relativity.API;

namespace FolderReplicator
{
    [kCura.EventHandler.CustomAttributes.Description("Folder Replicator Post-Workspace Create")]
    [kCura.EventHandler.CustomAttributes.RunOnce(false)]
    [System.Runtime.InteropServices.Guid("ADC6C1E0-BAE1-43D4-A726-1D1FA8CE7B70")]
    class WorkspaceCreate : PostWorkspaceCreateEventHandlerBase
    {
        private IAPILog _logger;
        public static Response retVal = new Response();

        public override Response Execute()
        {
            //Get the logger from the helper and set the ForContext to this class.
            _logger = Helper.GetLoggerFactory().GetLogger().ForContext<WorkspaceCreate>();

            // construct a response object with default values
            retVal.Message = String.Empty;
            retVal.Success = true;
            
            try
            {
                // get the current workspace artifact ID
                int currentWorkspaceID = Helper.GetActiveCaseID();

                // get the current workspace database context
                IDBContext workspaceDBContext = Helper.GetDBContext(currentWorkspaceID);

                // query for template workspace artifactID
                int templateWorkspaceID = GetTemplateCase(workspaceDBContext);

                using (IRSAPIClient proxy = Helper.GetServicesManager().CreateProxy<IRSAPIClient>(ExecutionIdentity.System))
                {
                    // query for template workspace
                    DTOs.Workspace template = FindWorkspaceArtifactID(templateWorkspaceID, proxy);

                    if (template != null)
                    {
                        Folder._templateRootFolder = template.RootFolderID;
                    }
                    else
                    {
                        retVal.Success = false;
                        retVal.Message = "Template workspace not found; unable to replicate folder structure.";
                        return retVal;
                    }

                    // query for target workspace
                    DTOs.Workspace target = FindWorkspaceArtifactID(currentWorkspaceID, proxy);

                    if (target != null)
                    {
                        Folder._targetRootFolder = target.RootFolderID;
                    }
                    else
                    {
                        retVal.Success = false;
                        retVal.Message = "Target workspace not found; unable to replicate folder structure.";
                        _logger.LogError("Target workspace not found; unable to replicate folder structure.");
                        return retVal;
                    }

                    proxy.APIOptions.WorkspaceID = templateWorkspaceID;

                    // get folders from template workspace
                    List<DTOs.Result<DTOs.Folder>> source = Program.GetSourceFolders(proxy);

                    if (source == null)
                    {
                        retVal.Success = false;
                        retVal.Message = "Query for folders in template workspace was unsuccessful.";
                        _logger.LogError("Query for folders in template workspace was unsuccessful.");
                        return retVal;
                    }
                    else if (source.Count == 1)
                    {
                        retVal.Success = false;
                        retVal.Message = "No folders found in template workspace.";
                        _logger.LogError("No folders found in template workspace.");
                        return retVal;
                    }

                    proxy.APIOptions.WorkspaceID = currentWorkspaceID;

                    // create folders
                    Folder.CreateFolders(source, proxy);
                }

                
            }
            catch (Exception ex)
            {
                // catch an exception if it occurs, log it, and return a response with success = false.
                retVal.Success = false;
                retVal.Message = ex.ToString();
            }

            return retVal;
        }

        private static int GetTemplateCase(Relativity.API.IDBContext context)
        {
            string getTemplate = 
                "SELECT TOP 1 [CaseTemplateID] " +
                "FROM [CaseEventHandlerHistory] " +
                "WHERE [CaseTemplateID] IS NOT NULL";

            SqlDataReader result = context.ExecuteSQLStatementAsReader(getTemplate);

            if (result.HasRows)
            {
                result.Read();
                return result.GetInt32(0);
            }

            return 0;
        }

        static DTOs.Workspace FindWorkspaceArtifactID(int artifactID, IRSAPIClient client)
        {
            client.APIOptions.WorkspaceID = -1;

            //build the query / condition
            DTOs.Query<DTOs.Workspace> query = new DTOs.Query<DTOs.Workspace>
            {
                Condition = new WholeNumberCondition("Artifact ID", NumericConditionEnum.EqualTo, artifactID),
                Fields = DTOs.FieldValue.AllFields
            };

            // query for the workspace
            DTOs.QueryResultSet<DTOs.Workspace> resultSet = new DTOs.QueryResultSet<DTOs.Workspace>();
            try
            {
                resultSet = client.Repositories.Workspace.Query(query, 0);
            }
            catch
            {
                return null;
            }

            // check for success
            if (resultSet.Success)
            {
                if (resultSet.Results.Count > 0)
                {
                    DTOs.Workspace firstWorkspace = resultSet.Results.FirstOrDefault().Artifact;
                    return firstWorkspace;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
