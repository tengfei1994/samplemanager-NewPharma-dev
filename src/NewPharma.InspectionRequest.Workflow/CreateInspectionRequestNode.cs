using System;
using System.Globalization;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Internal.ObjectModel;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Server.Workflow.Attributes;
using Thermo.SampleManager.Server.Workflow.Definition;
using Thermo.SampleManager.Server.Workflow.Nodes;

namespace NewPharma.InspectionRequest.Workflow
{
    [WorkflowNode(
        InspectionRequestWorkflowConstants.CreateNodeType,
        "Create Inspection Request",
        "Inspection Request",
        "TEXT_TREE_PLAY",
        "Creates a new Inspection Request with a selected template and lifecycle",
        "WorkflowMessages",
        ValidForTypes = new[] { InspectionRequestWorkflowConstants.WorkflowType })]
    [Tag("ENTRY")]
    [Tag("DATACREATE")]
    [Tag("DATA")]
    [Follows(InspectionRequestWorkflowConstants.LoginNodeType)]
    [Unique]
    [CreateData(InspectionRequestWorkflowConstants.EntityName)]
    public class CreateInspectionRequestNode : CreateEntityNode
    {
        public CreateInspectionRequestNode(WorkflowNodeInternal definition)
            : base(definition)
        {
        }

        public override string AutoName()
        {
            return "Create Inspection Request";
        }

        public override void SetDefaultParameters()
        {
            base.SetDefaultParameters();
            EntityName = InspectionRequestWorkflowConstants.EntityName;
        }

        public override bool PerformNode()
        {
            TracePerformNode();

            string requestId = GenerateRequestId();
            IEntity entity = EntityManager.CreateEntity(
                InspectionRequestWorkflowConstants.EntityName,
                new Identity(requestId));

            entity.Set("REQUEST_ID", requestId);
            entity.Set("ID_TEXT", requestId);
            entity.Set("NAME", requestId);
            entity.Set("STATUS", "DRAFT");
            entity.Set("EXECUTION_STATUS", "NOT_STARTE");

            if (EntityTemplate is EntityTemplateInternal template)
            {
                if (!template.IsValid())
                {
                    AddError(GetMessage("CreateEntityInvalidTemplate"), Array.Empty<object>());
                    return false;
                }

                template.AssignPrePromptDefaults(entity);
                if (IsRehearsal)
                {
                    AddTemplatePrompts(template, entity);
                    return false;
                }

                if (!GetTemplateResponse(entity))
                {
                    AddError(GetMessage("CreateEntityCancelled"), Array.Empty<object>());
                    return false;
                }

                template.AssignPostPromptDefaults(entity);
            }

            Properties.Set(InspectionRequestWorkflowConstants.EntityName, entity);
            Properties.AddEntity(entity);
            EntityManager.Transaction.Add(entity);

            if (entity is BaseEntity baseEntity)
            {
                baseEntity.PushWorkflowJournal(
                    "CREATED",
                    string.Empty,
                    Properties.TimeWorkflowStarted,
                    Library.Environment.CurrentUser,
                    false);
            }

            return true;
        }

        private static string GenerateRequestId()
        {
            return "NPHIR" + DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        }
    }
}
