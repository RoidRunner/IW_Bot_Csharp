using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium.Shitposting
{
    static class QuoteService
    {
        private static List<Quote> QuoteList;
        private static int nextQuoteId;
        private static int ConsumeQuoteId
        {
            get
            {
                return nextQuoteId++;
            }
        }

        internal static Quote RandomQuote
        {
            get
            {
                if (QuoteList.Count == 0)
                {
                    return null;
                }
                else
                {
                    int index = Macros.Rand.Next(QuoteService.QuoteList.Count);
                    return QuoteList[index];
                }
            }
        }

        internal static Quote GetQuote(int Id)
        {
            if (Id > 0 && Id < QuoteList.Count)
            {
                return QuoteList[Id];
            }
            else
            {
                return null;
            }
        }

        internal static async Task AddQuote(Quote newQuote)
        {
            if (!HasQuote(newQuote.MessageId))
            {
                newQuote.Id = ConsumeQuoteId;
                QuoteList.Add(newQuote);
                await SafeQuote(QuoteList.Count - 1);
            }
        }

        internal static bool HasQuote(ulong MessageId)
        {
            foreach (Quote quote in QuoteList)
            {
                if (quote.MessageId == MessageId)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool HasQuote(int QuoteId)
        {
            if (QuoteId >= nextQuoteId)
            {
                return false;
            }
            foreach (Quote quote in QuoteList)
            {
                if (quote.Id == QuoteId)
                {
                    return true;
                }
            }
            return false;
        }

        internal static async Task RemoveQuote(int QuoteId)
        {
            foreach (Quote quote in QuoteList)
            {
                if (quote.Id == QuoteId)
                {
                    QuoteList.Remove(quote);
                    break;
                }
            }
            await SafeQuote();
        }

        static QuoteService()
        {
            QuoteList = new List<Quote>();
        }

        private const int QUOTE_PAGESIZE = 64;
        private const string JSON_QUOTEID = "QuoteId";

        internal static async Task SafeQuote(int listLocation = -1)
        {
            JSONObject quoteSettings = new JSONObject();
            quoteSettings.AddField(JSON_QUOTEID, nextQuoteId);
            await ResourcesModel.WriteJSONObjectToFile(ResourcesModel.QuoteSettingsFilePath, quoteSettings);

            if (listLocation == -1)
            {
                foreach (string file in Directory.GetFiles(ResourcesModel.QuotesDirectory))
                {
                    if (file.Contains("quotes-") && file.EndsWith(".json"))
                    {
                        File.Delete(file);
                    }
                }
                int pages = (QuoteList.Count - 1) / QUOTE_PAGESIZE;
                for (int i = 0; i <= pages; i++)
                {
                    await SafeQuotePage(i);
                }
            }
            else
            {
                int page = listLocation / QUOTE_PAGESIZE;
                await SafeQuotePage(page);
            }
        }

        internal static async Task SafeQuotePage(int page)
        {
            JSONObject quoteListJSON = new JSONObject();
            for (int i = page * QUOTE_PAGESIZE; i < QuoteList.Count && i < (page + 1) * QUOTE_PAGESIZE; i++)
            {
                quoteListJSON.Add(QuoteList[i].ToJSON());
            }
            await ResourcesModel.WriteJSONObjectToFile(string.Format("{0}quotes-{1}.json", ResourcesModel.QuotesDirectory, page), quoteListJSON);
        }

        internal static async Task<bool> LoadQuotes()
        {
            LoadFileOperation quoteSettings = await ResourcesModel.LoadToJSONObject(ResourcesModel.QuoteSettingsFilePath);

            if (quoteSettings.Success)
            {
                if (quoteSettings.Result.GetField(ref nextQuoteId, JSON_QUOTEID))
                {

                    string[] files = Directory.GetFiles(ResourcesModel.QuotesDirectory);
                    foreach (string filename in files)
                    {
                        if (filename.EndsWith("json") && filename.Contains("quotes-"))
                        {
                            LoadFileOperation QuoteFile = await ResourcesModel.LoadToJSONObject(filename);
                            if (QuoteFile.Success)
                            {
                                handleQuoteJSON(QuoteFile.Result);
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private static void handleQuoteJSON(JSONObject quoteFile)
        {
            if (quoteFile.IsArray && quoteFile.Count > 0)
            {
                foreach (JSONObject quoteJSON in quoteFile)
                {
                    Quote loadedQuote = new Quote();
                    if (loadedQuote.FromJSON(quoteJSON))
                    {
                        if (!HasQuote(loadedQuote.Id))
                        {
                            QuoteList.Add(loadedQuote);
                        }
                    }
                }
            }
        }
    }
}
