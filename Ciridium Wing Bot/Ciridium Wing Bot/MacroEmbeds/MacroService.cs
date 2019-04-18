using Ciridium.Shitposting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium.MacroEmbeds
{
    class MacroService
    {
        private static PagedStorageService<MacroEmbed> macroStorage;
        private static Dictionary<string, MacroEmbed> macroEmbeds = new Dictionary<string, MacroEmbed>();

        internal static int Count
        {
            get
            {
                return macroStorage.Count;
            }
        }

        internal static IReadOnlyList<MacroEmbed> Macros
        {
            get {
                return macroStorage.StoredEntries;
            }
        }

        internal static MacroEmbed GetMacroEmbed(int Id)
        {
            return macroStorage[Id];
        }

        internal static MacroEmbed GetMacroEmbed(string key)
        {
            if (macroEmbeds.TryGetValue(key, out MacroEmbed macroEmbed))
            {
                return macroEmbed;
            }
            else
            {
                return null;
            }
        }

        internal static async Task<bool> AddMacroEmbed(MacroEmbed newMacroEmbed)
        {
            if (!macroEmbeds.ContainsKey(newMacroEmbed.Key))
            {
                await macroStorage.AddEntry(newMacroEmbed);
                macroEmbeds.Add(newMacroEmbed.Key, newMacroEmbed);
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool HasMacroEmbed(int id)
        {
            return macroStorage.HasEntryWithId(id);
        }

        internal static bool HasMacroEmbed(string key)
        {
            return macroEmbeds.ContainsKey(key);
        }

        internal static async Task RemoveMacroEmbed(int id)
        {
            MacroEmbed toBeRemoved = macroStorage[id];
            if (toBeRemoved != null)
            {
                macroEmbeds.Remove(toBeRemoved.Key);
                await macroStorage.RemoveEntry(id);
            }
        }

        static MacroService()
        {
            macroStorage = new PagedStorageService<MacroEmbed>(ResourcesModel.MacroEmbedsDirectory);
        }

        internal static async Task Initialize()
        {
            await macroStorage.InitialLoad();
            foreach (MacroEmbed macroEmbed in macroStorage.StoredEntries)
            {
                macroEmbeds.Add(macroEmbed.Key, macroEmbed);
            }
        }
    }
}
