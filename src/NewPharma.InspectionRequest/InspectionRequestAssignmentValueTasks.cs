using System;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Core.Definition;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Tasks;

namespace NewPharma.InspectionRequest
{
    public abstract class InspectionRequestAssignmentValueTask : SampleManagerTask
    {
        protected abstract string PropertyName { get; }

        protected override void SetupTask()
        {
            base.SetupTask();

            IEntity request = Context?.SelectedItems?.Count > 0
                ? Context.SelectedItems[0] as IEntity
                : null;

            if (request == null || !request.IsValid())
            {
                Library.Utils.FlashMessage("Select one Inspection Request before setting a data assignment value.", "Inspection Request");
                Exit(false);
                return;
            }

            try
            {
                string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
                IEntity field = SelectFirstField(requestId, PropertyName);
                if (field == null || !field.IsValid())
                {
                    Library.Utils.FlashMessage($"No Data Assignment field named {PropertyName} was found for this Inspection Request.", "Inspection Request");
                    Exit(false);
                    return;
                }

                string tableName = ResolveEntryTable(field);
                ISchemaField schemaField = FindSchemaField(tableName, PropertyName);
                if (schemaField?.LinkTable == null)
                {
                    Library.Utils.FlashMessage($"{PropertyName} is not configured as a linked entity field. Please key in the value directly in Data Assignment.", "Inspection Request");
                    Exit(false);
                    return;
                }

                Library.Utils.PromptForEntity(schemaField.LinkTable.Name, out IEntity selected);
                if (selected == null || !selected.IsValid())
                {
                    Exit(false);
                    return;
                }

                string selectedIdentity = ToIdentityText(selected);
                field.Set("OVERRIDE_VALUE", selectedIdentity);
                EntityManager.Transaction.Add(field);
                EntityManager.Commit();

                Library.Utils.FlashMessage($"{PropertyName} has been updated to {selectedIdentity}. Reopen or refresh the Inspection Request to see the value in Data Assignment.", "Inspection Request");
                Exit(true);
            }
            catch (Exception ex)
            {
                Library.Utils.FlashMessage(ex.Message, "Inspection Request");
                Exit(false);
            }
        }

        private IEntity SelectFirstField(string requestId, string propertyName)
        {
            IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.TableIrLoginPlanField);
            query.AddEquals("REQUEST_ID", requestId);
            query.AddAnd();
            query.AddEquals("PROPERTY", propertyName);

            foreach (IEntity field in EntityManager.Select(query))
            {
                return field;
            }

            return null;
        }

        private string ResolveEntryTable(IEntity field)
        {
            IQuery query = EntityManager.CreateQuery(InspectionRequestConstants.TableIrLoginPlanEntry);
            query.AddEquals("REQUEST_ID", GetString(field, "REQUEST_ID"));
            query.AddAnd();
            query.AddEquals("ORDER_NUMBER", GetString(field, "PARENT_ORDER_NUMBER"));

            foreach (IEntity entry in EntityManager.Select(query))
            {
                return GetString(entry, "TABLE_NAME");
            }

            return string.Empty;
        }

        private ISchemaField FindSchemaField(string tableName, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(tableName) ||
                string.IsNullOrWhiteSpace(propertyName) ||
                !Library.Schema.Tables.Contains(tableName))
            {
                return null;
            }

            ISchemaTable schemaTable = Library.Schema.Tables[tableName];
            return schemaTable.GetFieldFromProperty(propertyName);
        }

        private static string GetString(IEntity entity, string fieldName)
        {
            try
            {
                return entity.Get(fieldName)?.ToString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ToIdentityText(IEntity entity)
        {
            try
            {
                return entity.Identity;
            }
            catch
            {
                return entity?.ToString() ?? string.Empty;
            }
        }
    }

    [SampleManagerTask("NewPharmaSetIrProjectIdTask")]
    public sealed class SetInspectionRequestProjectIdTask : InspectionRequestAssignmentValueTask
    {
        protected override string PropertyName => "ProjectId";
    }

    [SampleManagerTask("NewPharmaSetIrProductLinkTask")]
    public sealed class SetInspectionRequestProductLinkTask : InspectionRequestAssignmentValueTask
    {
        protected override string PropertyName => "ProductLink";
    }

    [SampleManagerTask("NewPharmaSetIrSamplingPointTask")]
    public sealed class SetInspectionRequestSamplingPointTask : InspectionRequestAssignmentValueTask
    {
        protected override string PropertyName => "SamplingPoint";
    }
}
