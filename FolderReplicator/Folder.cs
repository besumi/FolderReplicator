using System;
using System.Collections.Generic;
using System.Linq;
using DTOs = kCura.Relativity.Client.DTOs;
using kCura.Relativity.Client;
using Relativity.API;

namespace FolderReplicator
{
    class Folder
    {
        public static int? _templateRootFolder = 0;
        public static int? _targetRootFolder = 0;
        static List<DTOs.Folder> _createdFolders = new List<DTOs.Folder>();

        public static void CreateFolders(List<DTOs.Result<DTOs.Folder>> folders, IRSAPIClient client)
        {

            // store all top-level folders
            List<DTOs.Result<DTOs.Folder>> topLevel = folders.FindAll(x => x.Artifact.ParentArtifact.ArtifactID == _templateRootFolder);

            // create all top-level folders
            foreach (DTOs.Result<DTOs.Folder> f in topLevel)
            {
                DTOs.Folder current = f.Artifact;
                DTOs.Folder newFolder = CreateFolder(f.Artifact, (int)_targetRootFolder, client);
                if (newFolder != null)
                {
                    // add to running total of created folders
                    _createdFolders.Add(current);
                    // remove from list of folders to create
                    folders.Remove(f);
                    // if there are more folders to create, recurse
                    if (folders.Count > 0)
                        CheckForChildren(current, newFolder, folders, client);
                }
                else
                {
                    WorkspaceCreate.retVal.Message = String.Format("Error occurred creating folder {0}", f.Artifact.Name);
                }
            }

            //_logger.LogDebug(String.Format("\r\n{0} total folders created.", _createdFolders.Count));
        }

        static DTOs.Folder CreateFolder(DTOs.Folder source, int parent, IRSAPIClient client)
        {
            source.Name = source.Name;
            source.ParentArtifact = new DTOs.Artifact(parent);

            DTOs.WriteResultSet<DTOs.Folder> results;

            try
            {
                results = client.Repositories.Folder.Create(source);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("An error occurred: {0}", ex.Message);
                return null;
            }

            if (!results.Success)
            {
                //Console.WriteLine("Error: " + results.Results.FirstOrDefault().Message);
                return null;
            }
            else
            {
                //Console.Write("{0} folder created successfully.\r\n", source.Name);
                return results.Results.FirstOrDefault().Artifact;
            }
        }

        static void CheckForChildren(DTOs.Folder source, DTOs.Folder target, List<DTOs.Result<DTOs.Folder>> folders, IRSAPIClient client)
        {
            // see if this folder has children
            if (folders.Exists(x => x.Artifact.ParentArtifact.ArtifactID == source.ArtifactID))
            {
                // store all the children in a list
                List<DTOs.Result<DTOs.Folder>> children = folders.FindAll(x => x.Artifact.ParentArtifact.ArtifactID == source.ArtifactID);

                foreach (DTOs.Result<DTOs.Folder> f in children)
                {
                    // create a new folder
                    DTOs.Folder newFolder = CreateFolder(f.Artifact, target.ArtifactID, client);
                    // add to list of created folders
                    _createdFolders.Add(f.Artifact);
                    // remove from list to create
                    folders.Remove(f);
                    // if there are more to do, recurse
                    if (folders.Count > 0)
                        CheckForChildren(f.Artifact, newFolder, folders, client);
                }
            }
        }
    }
}
