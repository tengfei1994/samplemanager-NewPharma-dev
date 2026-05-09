using System;
using Thermo.SampleManager.Library;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Tasks;

namespace NewPharma.InspectionRequest
{
    /// <summary>
    /// Standard LabTable maintenance entry for the NewPharma Inspection Request table.
    /// </summary>
    [SampleManagerTask("NewPharmaInspectionRequestTask", "LABTABLE", InspectionRequestConstants.EntityName)]
    public class InspectionRequestLabTableTask : GenericLabtableTask
    {
        private bool _snapshotRefreshRequested;

        protected override void MainFormCreated()
        {
            base.MainFormCreated();

            if (!string.Equals(Context.LaunchMode, "ADD", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (MainForm?.Entity is not IEntity request)
            {
                return;
            }

            string requestId = GenerateRequestId();

            SetIfEmpty(request, InspectionRequestConstants.FieldRequestId, requestId);
            SetIfEmpty(request, InspectionRequestConstants.FieldIdText, requestId);
            SetIfEmpty(request, InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusDraft);
            SetIfEmpty(request, InspectionRequestConstants.FieldExecutionStatus, InspectionRequestConstants.ExecutionNotExecuted);
            SetIfEmpty(request, InspectionRequestConstants.FieldUseLastActiveVersion, true);
            SetIfEmpty(request, InspectionRequestConstants.FieldEsigRequired, false);
            SetIfEmpty(request, InspectionRequestConstants.FieldRequestedOn, DateTime.Now);

            if (Library.Environment.CurrentUser != null)
            {
                SetIfEmpty(request, InspectionRequestConstants.FieldRequestedBy, Library.Environment.CurrentUser);
            }

            Library.Task.StateModified();
        }

        protected override void MainFormLoaded()
        {
            base.MainFormLoaded();

            if (MainForm?.Entity is IEntity request)
            {
                request.PropertyChanged += Request_PropertyChanged;
            }
        }

        protected override bool OnPreSave()
        {
            if (MainForm?.Entity is IEntity request)
            {
                EnsureRequestDefaults(request);
                SyncLoginPlanSelection(request);
                ValidateRequest(request);
            }

            return base.OnPreSave();
        }

        protected override void OnPostSave()
        {
            base.OnPostSave();

            if (MainForm?.Entity is IEntity request &&
                (string.Equals(Context.LaunchMode, "ADD", StringComparison.OrdinalIgnoreCase) || _snapshotRefreshRequested))
            {
                var snapshotService = new InspectionRequestSnapshotService(EntityManager);
                snapshotService.EnsureSnapshot(request, _snapshotRefreshRequested);
                _snapshotRefreshRequested = false;
            }
        }

        private void Request_PropertyChanged(object sender, PropertyEventArgs e)
        {
            if (string.Equals(e.PropertyName, "LoginPlan", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldLoginPlanId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldLoginPlanIdProperty, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldLoginPlanVersionProperty, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.PropertyName, InspectionRequestConstants.FieldLoginPlanVersion, StringComparison.OrdinalIgnoreCase))
            {
                _snapshotRefreshRequested = true;
            }
        }

        private static void EnsureRequestDefaults(IEntity request)
        {
            string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
            if (string.IsNullOrWhiteSpace(requestId) || string.Equals(requestId, "TBD", StringComparison.OrdinalIgnoreCase))
            {
                requestId = GenerateRequestId();
                request.Set(InspectionRequestConstants.FieldRequestId, requestId);
            }

            SetIfEmpty(request, InspectionRequestConstants.FieldIdText, requestId);
            SetIfEmpty(request, InspectionRequestConstants.FieldStatus, InspectionRequestConstants.StatusDraft);
            SetIfEmpty(request, InspectionRequestConstants.FieldExecutionStatus, InspectionRequestConstants.ExecutionNotExecuted);
            SetIfEmpty(request, InspectionRequestConstants.FieldUseLastActiveVersion, true);
        }

        private void ValidateRequest(IEntity request)
        {
            if (string.IsNullOrWhiteSpace(GetLoginPlanIdentity(request)))
            {
                Library.Utils.FlashMessage("Please select a Login Plan before saving the Inspection Request.", "Inspection Request");
                throw new InvalidOperationException("Inspection Request requires a Login Plan.");
            }
        }

        private static void SyncLoginPlanSelection(IEntity request)
        {
            string loginPlanId = GetLoginPlanIdentity(request);
            string loginPlanVersion = GetFirstString(
                request,
                InspectionRequestConstants.FieldLoginPlanVersionProperty,
                InspectionRequestConstants.FieldLoginPlanVersion);

            object selectedLoginPlan = TryGet(request, InspectionRequestConstants.FieldLoginPlan);
            if (selectedLoginPlan is IEntity loginPlan)
            {
                loginPlanId = ToIdentityText(loginPlan);
                loginPlanVersion = GetFirstString(loginPlan, "Version", "VERSION");
            }

            if (!string.IsNullOrWhiteSpace(loginPlanId))
            {
                SetIfDifferent(request, InspectionRequestConstants.FieldLoginPlanId, loginPlanId);
                SetIfDifferent(request, InspectionRequestConstants.FieldLoginPlanIdProperty, loginPlanId);
            }

            if (!string.IsNullOrWhiteSpace(loginPlanVersion))
            {
                SetIfDifferent(request, InspectionRequestConstants.FieldLoginPlanVersion, loginPlanVersion);
                SetIfDifferent(request, InspectionRequestConstants.FieldLoginPlanVersionProperty, loginPlanVersion);
            }
        }

        internal static string GetLoginPlanIdentity(IEntity request)
        {
            object selectedLoginPlan = TryGet(request, InspectionRequestConstants.FieldLoginPlan);
            if (selectedLoginPlan is IEntity loginPlan)
            {
                return ToIdentityText(loginPlan);
            }

            return GetFirstString(
                request,
                InspectionRequestConstants.FieldLoginPlanIdProperty,
                InspectionRequestConstants.FieldLoginPlanId,
                InspectionRequestConstants.FieldLoginPlan);
        }

        internal static string GetLoginPlanVersion(IEntity request)
        {
            object selectedLoginPlan = TryGet(request, InspectionRequestConstants.FieldLoginPlan);
            if (selectedLoginPlan is IEntity loginPlan)
            {
                return GetFirstString(loginPlan, "Version", "VERSION");
            }

            return GetFirstString(
                request,
                InspectionRequestConstants.FieldLoginPlanVersionProperty,
                InspectionRequestConstants.FieldLoginPlanVersion);
        }

        private static void SetIfEmpty(IEntity entity, string fieldName, object value)
        {
            object current = TryGet(entity, fieldName);
            if (current == null || string.IsNullOrWhiteSpace(current.ToString()))
            {
                TrySet(entity, fieldName, value);
            }
        }

        private static void SetIfDifferent(IEntity entity, string fieldName, object value)
        {
            object current = TryGet(entity, fieldName);
            if (!string.Equals(current?.ToString() ?? string.Empty, value?.ToString() ?? string.Empty, StringComparison.Ordinal))
            {
                TrySet(entity, fieldName, value);
            }
        }

        private static object TryGet(IEntity entity, string fieldName)
        {
            try
            {
                return entity.Get(fieldName);
            }
            catch
            {
                return null;
            }
        }

        private static void TrySet(IEntity entity, string fieldName, object value)
        {
            try
            {
                entity.Set(fieldName, value);
            }
            catch
            {
            }
        }

        private static string GetString(IEntity entity, string fieldName)
        {
            object value = TryGet(entity, fieldName);
            return value?.ToString() ?? string.Empty;
        }

        private static string GetFirstString(IEntity entity, params string[] fieldNames)
        {
            foreach (string fieldName in fieldNames)
            {
                string value = GetString(entity, fieldName);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return string.Empty;
        }

        private static string ToIdentityText(IEntity entity)
        {
            return entity?.Identity?.ToString() ?? string.Empty;
        }

        private static string GenerateRequestId()
        {
            return "NPHIR" + DateTime.Now.ToString("yyyyMMddHHmmss");
        }
    }
}
