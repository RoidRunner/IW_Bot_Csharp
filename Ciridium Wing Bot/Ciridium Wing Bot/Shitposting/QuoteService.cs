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
        private static PagedStorageService<Quote> quoteStorage;

        internal static Quote RandomQuote
        {
            get
            {
                if (quoteStorage.Count == 0)
                {
                    return null;
                }
                else
                {
                    int index = Macros.Rand.Next(quoteStorage.Count);
                    return quoteStorage[index];
                }
            }
        }

        internal static int Count
        {
            get
            {
                return quoteStorage.Count;
            }
        }

        internal static Quote GetQuote(int Id)
        {
            return quoteStorage[Id];
        }

        internal static async Task<bool> AddQuote(Quote newQuote)
        {
            if (!HasQuote(newQuote.MessageId))
            {
                await quoteStorage.AddEntry(newQuote);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool HasQuote(ulong MessageId)
        {
            foreach (Quote quote in quoteStorage.StoredEntries)
            {
                if (quote.MessageId == MessageId)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool HasQuote(int id)
        {
            return quoteStorage.HasEntryWithId(id);
        }

        internal static async Task RemoveQuote(int id)
        {
            await quoteStorage.RemoveEntry(id);
        }

        static QuoteService()
        {
            quoteStorage = new PagedStorageService<Quote>(ResourcesModel.QuotesDirectory);
        }

        internal static async Task Initialize()
        {
            await quoteStorage.InitialLoad();
        }
    }
}
