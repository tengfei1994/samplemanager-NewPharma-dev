using System;
using System.Collections.Generic;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Tasks;

namespace NewPharma.InspectionRequest;

/// <summary>
/// One-shot background task that creates the phrase types used by NPH_INSPECTION_REQUEST.
/// </summary>
[SampleManagerTask("NewPharmaConfigureInspectionRequestPhrases")]
public class ConfigureInspectionRequestPhrasesTask : SampleManagerTask
{
    protected override void SetupTask()
    {
        base.SetupTask();

        EnsurePhraseType(
            "NPH_IR_STA",
            "NewPharma Inspection Request Status",
            new[]
            {
                ("DRAFT", "Draft"),
                ("SUBMITTED", "Submitted"),
                ("UNDER_REVI", "Under Review"),
                ("APPROVED", "Approved"),
                ("REJECTED", "Rejected"),
                ("CANCELLED", "Cancelled"),
                ("EXECUTING", "Executing"),
                ("EXECUTED", "Executed"),
                ("EXECUTION_", "Execution Failed")
            });

        EnsurePhraseType(
            "NPH_IR_EXE",
            "NewPharma Inspection Request Execution Status",
            new[]
            {
                ("NOT_STARTE", "Not Started"),
                ("EXECUTING", "Executing"),
                ("EXECUTED", "Executed"),
                ("FAILED", "Failed")
            });

        EntityManager.Commit();
        Exit(true);
    }

    private void EnsurePhraseType(string identity, string description, IReadOnlyList<(string Id, string Text)> phrases)
    {
        var header = EntityManager.Select("PHRASE_HEADER", new Identity(identity)) as IEntity;
        if (!(header?.IsValid() ?? false))
        {
            header = EntityManager.CreateEntity("PHRASE_HEADER");
            header.Set("IDENTITY", identity);
            header.Set("NAME", description);
            header.Set("DESCRIPTION", description);
            header.Set("MODIFIABLE", true);
            EntityManager.Transaction.Add(header);
        }

        for (var i = 0; i < phrases.Count; i++)
        {
            EnsurePhrase(identity, phrases[i].Id, phrases[i].Text, i + 1);
        }
    }

    private void EnsurePhrase(string phraseType, string phraseId, string phraseText, int orderNumber)
    {
        var phrase = EntityManager.Select("PHRASE", new Identity(phraseType, phraseId)) as IEntity;
        if (!(phrase?.IsValid() ?? false))
        {
            phrase = EntityManager.CreateEntity("PHRASE");
            phrase.Set("PHRASE_TYPE", phraseType);
            phrase.Set("PHRASE_ID", phraseId);
            phrase.Set("ORDER_NUM", orderNumber);
            phrase.Set("PHRASE_TEXT", phraseText);
            EntityManager.Transaction.Add(phrase);
            return;
        }

        phrase.Set("ORDER_NUM", orderNumber);
        phrase.Set("PHRASE_TEXT", phraseText);
        EntityManager.Transaction.Add(phrase);
    }
}
