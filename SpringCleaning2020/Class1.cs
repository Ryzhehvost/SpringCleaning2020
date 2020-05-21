using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using ArchiSteamFarm;
using ArchiSteamFarm.Collections;
using ArchiSteamFarm.Json;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Plugins;
using JetBrains.Annotations;
using SteamKit2;


namespace SpringCleaning2020
{
	[Export(typeof(IPlugin))]
    public class Class1 : IBotCommand {
        public string Name => "SpringCleaning2020";
        public Version Version => typeof(Class1).Assembly.GetName().Version;


		private static async Task<string> DoCleaning(ulong steamID, Bot bot) {

			const string post_request = "/springcleaning/ajaxoptintoevent";
			const string html_request = "/springcleaning";

			await bot.ArchiWebHandler.UrlPostWithSession(ArchiWebHandler.SteamStoreURL, post_request).ConfigureAwait(false);

			IDocument html =  await bot.ArchiWebHandler.UrlPostToHtmlDocumentWithSession(ArchiWebHandler.SteamStoreURL, html_request).ConfigureAwait(false);

			IHtmlCollection<IElement> coll = html.DocumentElement.QuerySelectorAll(".task_dialog_row > div > div > div:nth-child(1)");
			IEnumerable<uint> appids = coll.Select(x => uint.Parse(x.GetAttributeValue("data-sg-appid"))).Distinct();

			string query = "addlicense "+string.Join(",",appids.Select(x => "a/" + x.ToString()));
			await bot.Commands.Response(steamID, query).ConfigureAwait(false);
			await bot.Actions.Play(appids).ConfigureAwait(false);
			bot.Actions.Resume();
			return "Done!";
		}
		private static async Task<string> ResponseCleaning(ulong steamID, string botNames) {
			HashSet<Bot> bots = Bot.GetBots(botNames);
			if ((bots == null) || (bots.Count == 0)) {
				return Commands.FormatStaticResponse(string.Format(Strings.BotNotFound, botNames));
			}

			IList<string> results = await Utilities.InParallel(bots.Select(bot => DoCleaning(steamID,bot))).ConfigureAwait(false);

			List<string> responses = new List<string>(results.Where(result => !string.IsNullOrEmpty(result)));

			return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
		}
		public async Task<string> OnBotCommand([NotNull] Bot bot, ulong steamID, [NotNull] string message, [ItemNotNull, NotNull] string[] args) {
			if (!bot.HasPermission(steamID, BotConfig.EPermission.Master)) {
				return null;
			}

			switch (args[0].ToUpperInvariant()) {
				case "SPRINGCLEANING" when args.Length > 1:
					return await ResponseCleaning(steamID, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
				case "SPRINGCLEANING":
					return await ResponseCleaning(steamID, bot.BotName).ConfigureAwait(false);
				default:
					return null;
			}

		}
		public void OnLoaded() => ASF.ArchiLogger.LogGenericInfo("SpringCleaning2020 Plugin by Ryzhehvost, powered by ginger cats");
	}
}
