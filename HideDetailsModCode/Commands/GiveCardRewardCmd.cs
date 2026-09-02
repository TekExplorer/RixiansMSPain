using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace HideDetailsMod.HideDetailsModCode.Commands;

class GiveCardRewardCmd : AbstractConsoleCmd
{
    public override string CmdName => "givecardreward";

    public override string Args => "[card-id:string] [card-id:string] [card-id:string]";

    public override string Description => "Gives a specified card reward";

    public override bool IsNetworked => true;
    public override bool DebugOnly => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (!RunManager.Instance.IsInProgress) return new(false, "A run must be in progress");
        if (issuingPlayer is null) return new(false, "A player could not be found");

        if (args.IsEmpty())
        {
            return RandomCards(issuingPlayer);
        }
        return SpecificCards(issuingPlayer, args);
    }

    private CmdResult RandomCards(Player player)
    {
        CardCreationOptions options = new([player.Character.CardPool], CardCreationSource.Other, CardRarityOddsType.RegularEncounter);
        var task = RewardsCmd.OfferCustom(player, [new CardReward(options, 3, player)]);
        return new(task, true, "Provided card reward");
    }

    private CmdResult SpecificCards(Player issuingPlayer, string[] args)
    {
        List<CardModel> cards = [];
        foreach (var arg in args)
        {
            var card = FindCard(arg);
            if (card == null) return CardNotFound(arg);
            cards.Add(card);
        }

        while (cards.Count < 3)
        {
            cards.Add(cards[0]);
        }

        CardCreationOptions options = new([issuingPlayer.Character.CardPool], CardCreationSource.Other, CardRarityOddsType.RegularEncounter);

        CardReward cardReward = new(cards, CardCreationSource.Encounter, issuingPlayer, options);

        var task = RewardsCmd.OfferCustom(issuingPlayer, [cardReward]);
        return new(task, true, "Provided custom card reward");
    }

    CardModel? FindCard(string cardName)
    {
        cardName = cardName.ToUpperInvariant();
        return ModelDb.AllCards.FirstOrDefault(c => c.Id.Entry == cardName);
    }

    CmdResult CardNotFound(string cardName)
    {
        return new CmdResult(success: false, "Card '" + cardName + "' not found");
    }


    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 3)
        {
            var candidates = ModelDb.AllCards.Select(card => card.Id.Entry);
            return CompleteArgument(candidates, [], args.FirstOrDefault() ?? "");
        }

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName
        };
    }
}