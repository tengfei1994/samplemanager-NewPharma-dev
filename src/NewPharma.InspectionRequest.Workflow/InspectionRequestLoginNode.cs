using Thermo.SampleManager.Server.Workflow.Attributes;
using Thermo.SampleManager.Server.Workflow.Definition;
using Thermo.SampleManager.Server.Workflow.Nodes;

namespace NewPharma.InspectionRequest.Workflow
{
    [WorkflowRootNode(
        InspectionRequestWorkflowConstants.LoginNodeType,
        "Inspection Request Create Workflow",
        InspectionRequestWorkflowConstants.WorkflowType,
        "Inspection Request",
        "TEXT_TREE_PLAY",
        "Root node for Inspection Request create workflows",
        "WorkflowMessages")]
    [WorkflowType(
        InspectionRequestWorkflowConstants.WorkflowType,
        InspectionRequestWorkflowConstants.AddMenu,
        InspectionRequestWorkflowConstants.DisplayMenu,
        InspectionRequestWorkflowConstants.ModifyMenu,
        InspectionRequestWorkflowConstants.CopyMenu,
        true)]
    [Tag("ENTRY")]
    public class InspectionRequestLoginNode : Node
    {
        public InspectionRequestLoginNode(WorkflowNodeInternal definition)
            : base(definition)
        {
        }

        public string WorkflowTask => InspectionRequestWorkflowConstants.TaskName;

        public string WorkflowTaskActionType => "ADD";

        public string WorkflowTaskEntityType => InspectionRequestWorkflowConstants.EntityName;

        public string WorkflowTaskParameter => InspectionRequestWorkflowConstants.TaskParameter;

        public override string AutoName()
        {
            return "Inspection Request Create Workflow";
        }
    }
}
