using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium
{
    internal class PagedStorageService<T> where T : PageStorable, new()
    {
        internal readonly string StorageDirectory;

        private const int PAGESIZE = 64;
        private const string ID_SAFEFILE = "Id.json";
        private const string JSON_ID = "Id";
        private static List<T> pageStorables = new List<T>();
        private static int nextId;
        internal static int ConsumeId
        {
            get
            {
                return nextId++;
            }
        }

        public PagedStorageService(string storageDirectory)
        {
            StorageDirectory = storageDirectory;
        }

        internal IReadOnlyList<T> StoredEntries { get { return pageStorables.AsReadOnly(); } }
        internal int Count { get { return pageStorables.Count; } }

        internal async Task AddEntry(T entry)
        {
            entry.Id = ConsumeId;
            pageStorables.Add(entry);
            await SafePages(pageStorables.Count - 1);
        }

        internal async Task RemoveEntry(int id)
        {
            foreach (T entry in pageStorables)
            {
                if (entry.Id == id)
                {
                    pageStorables.Remove(entry);
                    break;
                }
            }
            await SafePages();
        }

        internal T this[int index]
        {
            get
            {
                if (index >= 0 && index < pageStorables.Count)
                {
                    return pageStorables[index];
                }
                else
                {
                    return default(T);
                }
            }
        }

        internal T GetEntry(int index)
        {
            return this[index];
        }

        internal bool HasEntryWithId(int id)
        {
            if (id >= nextId)
            {
                return false;
            }
            foreach (T entry in pageStorables)
            {
                if (entry.Id == id)
                {
                    return true;
                }
            }
            return false;
        }



        internal async Task SafePages(int listLocation = -1)
        {
            JSONObject idSettings = new JSONObject();
            idSettings.AddField(JSON_ID, nextId);
            await ResourcesModel.WriteJSONObjectToFile(StorageDirectory + ID_SAFEFILE, idSettings);

            if (listLocation == -1)
            {
                foreach (string file in Directory.GetFiles(ResourcesModel.QuotesDirectory))
                {
                    if (file.Contains("page-") && file.EndsWith(".json"))
                    {
                        File.Delete(file);
                    }
                }
                int pages = (pageStorables.Count - 1) / PAGESIZE;
                for (int i = 0; i <= pages; i++)
                {
                    await SafePage(i);
                }
            }
            else
            {
                int page = listLocation / PAGESIZE;
                await SafePage(page);
            }
        }

        internal async Task SafePage(int page)
        {
            JSONObject quoteListJSON = new JSONObject();
            for (int i = page * PAGESIZE; i < pageStorables.Count && i < (page + 1) * PAGESIZE; i++)
            {
                quoteListJSON.Add(pageStorables[i].ToJSON());
            }
            await ResourcesModel.WriteJSONObjectToFile(string.Format("{0}page-{1}.json", StorageDirectory, page), quoteListJSON);
        }

        internal async Task<bool> InitialLoad()
        {
            LoadFileOperation storageSettings = await ResourcesModel.LoadToJSONObject(StorageDirectory + ID_SAFEFILE);

            if (storageSettings.Success)
            {
                if (storageSettings.Result.GetField(ref nextId, JSON_ID))
                {

                    string[] files = Directory.GetFiles(StorageDirectory);
                    foreach (string filename in files)
                    {
                        if (filename.EndsWith("json") && filename.Contains("page-"))
                        {
                            LoadFileOperation PageFile = await ResourcesModel.LoadToJSONObject(filename);
                            if (PageFile.Success)
                            {
                                handlePageJSON(PageFile.Result);
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private void handlePageJSON(JSONObject page)
        {
            if (page.IsArray && page.Count > 0)
            {
                foreach (JSONObject pageEntry in page)
                {
                    T loadedStorable = new T();
                    if (loadedStorable.FromJSON(pageEntry) && loadedStorable.RetrieveId(pageEntry))
                    {
                        if (!HasEntryWithId(loadedStorable.Id))
                        {
                            pageStorables.Add(loadedStorable);
                        }
                    }
                }
            }
        }
    }

    internal abstract class PageStorable
    {
        internal int Id;

        internal bool RetrieveId(JSONObject json)
        {
            return json.GetField(ref Id, "Id");
        }
        protected JSONObject IdJSON
        {
            get
            {
                JSONObject json = new JSONObject();
                json.AddField("Id", Id);
                return json;
            }
        }
        internal abstract JSONObject ToJSON();
        internal abstract bool FromJSON(JSONObject json);
    }
}
