using System;
using System.Collections.Generic;
using System.Linq;
using kCura.Relativity.Client;
using DTOs = kCura.Relativity.Client.DTOs;

namespace FolderReplicator
{
    /// <summary>
    /// Console app to replicate folder structure.
    /// </summary>
    class Program
    {
        static string _rsapiUrl = "https://uuohpdpmuk7k.hopper.relativity.com";
        static string _rsapiUsername = "a@b.c";
        static string _rsapiPassword = "passwordHere";
        static string _templateCase = "Folder Template";
        static string _targetCase = "Folder Target";
        static int _templateArtifactId = 0;
        static int _targetArtifactId = 0;

        static void Main(string[] args)
        {
            Relativity.Services.ServiceProxy.ServiceFactorySettings settings = new Relativity.Services.ServiceProxy.ServiceFactorySettings(
                                                              new Uri(_rsapiUrl+"/relativity.services/"),
                                                              new Uri(_rsapiUrl +"/relativity.rest/api"),
                                                              new Relativity.Services.ServiceProxy.UsernamePasswordCredentials(_rsapiUsername, _rsapiPassword));

            DuplicateFolders(settings);


            // exit
            Console.WriteLine("\r\nDone.");
            Console.Read();
        }

        static void DuplicateFolders(Relativity.Services.ServiceProxy.ServiceFactorySettings settings)
        {
            try
            {
                using (IRSAPIClient rsapiProxy = new Relativity.Services.ServiceProxy.ServiceFactory(settings).CreateProxy<IRSAPIClient>())
                {
                    // query for template workspace
                    DTOs.Workspace template = FindWorkspace(_templateCase, rsapiProxy);

                    if (template != null)
                    {
                        _templateArtifactId = template.ArtifactID;
                        Folder._templateRootFolder = template.RootFolderID;
                    }
                    else { return; }

                    // query for target workspace
                    DTOs.Workspace target = FindWorkspace(_targetCase, rsapiProxy);

                    if (target != null)
                    {
                        _targetArtifactId = target.ArtifactID;
                        Folder._targetRootFolder = target.RootFolderID;
                    }
                    else { return; }

                    rsapiProxy.APIOptions.WorkspaceID = _templateArtifactId;

                    // get folders from template workspace
                    List<DTOs.Result<DTOs.Folder>> source = GetSourceFolders(rsapiProxy);

                    if (source == null)
                        return;
                    else if (source.Count == 1)
                    {
                        Console.WriteLine("Template workspace has no folders; exiting.");
                        return;
                    }

                    rsapiProxy.APIOptions.WorkspaceID = _targetArtifactId;

                    // confirm target workspace has no folders
                    bool? targetIsEmpty = VerifyEmptyTarget(rsapiProxy);

                    if (targetIsEmpty == false)
                    {
                        Console.WriteLine("Target workspace already contains folders; exiting.");
                        return;
                    }
                    else if (targetIsEmpty == null)
                        return;

                    // create folders
                    Folder.CreateFolders(source, rsapiProxy);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Exception encountered:\r\n{0}\r\n{1}", ex.Message, ex.InnerException));
            }
        }

        static DTOs.Workspace FindWorkspace(string name, IRSAPIClient client)
        {
            client.APIOptions.WorkspaceID = -1;

            //build the query / condition
            DTOs.Query<DTOs.Workspace> query = new DTOs.Query<DTOs.Workspace>
            {
                Condition = new TextCondition(DTOs.WorkspaceFieldNames.Name, TextConditionEnum.EqualTo, name),
                Fields = DTOs.FieldValue.AllFields
            };

            // query for the workspace
            DTOs.QueryResultSet<DTOs.Workspace> resultSet = new DTOs.QueryResultSet<DTOs.Workspace>();
            try
            {
                resultSet = client.Repositories.Workspace.Query(query, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Exception:\r\n{0}\r\n{1}", ex.Message, ex.InnerException));
                return null;
            }
            
            // check for success
            if (resultSet.Success)
            {
                if (resultSet.Results.Count > 0)
                {
                    DTOs.Workspace firstWorkspace = resultSet.Results.FirstOrDefault().Artifact;
                    Console.WriteLine(String.Format("Workspace found with artifactID {0}.", firstWorkspace.ArtifactID));
                    return firstWorkspace;
                }
                else
                {
                    Console.WriteLine("Query was successful but workspace does not exist.");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("Query was not successful.");
                return null;
            }
        }

        public static List<DTOs.Result<DTOs.Folder>> GetSourceFolders(IRSAPIClient client)
        {
            // build the query / condition
            DTOs.Query<DTOs.Folder> query = new DTOs.Query<DTOs.Folder>();
            query.Fields = DTOs.FieldValue.AllFields;

            // query for the folders
            DTOs.QueryResultSet<DTOs.Folder> resultSet = new DTOs.QueryResultSet<DTOs.Folder>();
            try
            {
                resultSet = client.Repositories.Folder.Query(query, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Exception:\r\n{0}\r\n{1}", ex.Message, ex.InnerException));
                return null;
            }

            // check for success
            if (resultSet.Success)
            {
                if (resultSet.Results.Count > 0)
                {
                    Console.WriteLine(String.Format("{0} folders found in {1}.\r\n", resultSet.Results.Count-1, _templateArtifactId));
                    return resultSet.Results;
                }
                else
                {
                    Console.WriteLine("Query was successful but no folders exist.");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("Query was not successful.");
                return null;
            }
        }

        static bool? VerifyEmptyTarget(IRSAPIClient client)
        {
            // build the query / condition
            DTOs.Query<DTOs.Folder> query = new DTOs.Query<DTOs.Folder>();

            // query for the folders
            DTOs.QueryResultSet<DTOs.Folder> resultSet = new DTOs.QueryResultSet<DTOs.Folder>();
            try
            {
                resultSet = client.Repositories.Folder.Query(query, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Exception:\r\n{0}\r\n{1}", ex.Message, ex.InnerException));
                return null;
            }

            // check for success
            if (resultSet.Success)
            {
                if (resultSet.Results.Count == 1)
                    return true;
                else
                    return false;
            }
            else
            {
                Console.WriteLine("Query was not successful.");
                return null;
            }
        }
        
    }
}
