using System;
using System.Globalization;
using Thermo.SampleManager.Common.Data;
using Thermo.SampleManager.Server;

namespace NewPharma.InspectionRequest
{
    internal sealed class InspectionRequestSnapshotService
    {
        private readonly IEntityManager _entityManager;

        public InspectionRequestSnapshotService(IEntityManager entityManager)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
        }

        public bool EnsureSnapshot(IEntity request, bool rebuild)
        {
            if (request == null)
            {
                return false;
            }

            string requestId = GetString(request, InspectionRequestConstants.FieldRequestId);
            string loginPlanId = InspectionRequestLabTableTask.GetLoginPlanIdentity(request);
            string loginPlanVersion = InspectionRequestLabTableTask.GetLoginPlanVersion(request);

            if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(loginPlanId))
            {
                return false;
            }

            loginPlanVersion = ResolveLoginPlanVersion(loginPlanId, loginPlanVersion);

            bool hasSnapshotRows = HasSnapshotRows(requestId);
            bool needsTemplateFieldBackfill = hasSnapshotRows && NeedsTemplateFieldBackfill(requestId);

            if (rebuild || needsTemplateFieldBackfill)
            {
                DeleteSnapshotRows(requestId);
            }
            else if (hasSnapshotRows)
            {
                return false;
            }

            CopyDataAssignment(requestId, loginPlanId, loginPlanVersion);
            CopyProductSpec(requestId, loginPlanId, loginPlanVersion);
            return true;
        }

        private string ResolveLoginPlanVersion(string loginPlanId, string loginPlanVersion)
        {
            if (!string.IsNullOrWhiteSpace(loginPlanVersion))
            {
                return loginPlanVersion;
            }

            IEntity loginPlan = _entityManager.SelectLatestVersion(
                InspectionRequestConstants.TableLoginPlan,
                loginPlanId) as IEntity;

            return loginPlan == null || !loginPlan.IsValid()
                ? string.Empty
                : GetString(loginPlan, "VERSION");
        }

        private void DeleteSnapshotRows(string requestId)
        {
            DeleteRows(InspectionRequestConstants.TableIrLoginPlanTestField, requestId);
            DeleteRows(InspectionRequestConstants.TableIrLoginPlanTest, requestId);
            DeleteRows(InspectionRequestConstants.TableIrLoginPlanField, requestId);
            DeleteRows(InspectionRequestConstants.TableIrLoginPlanEntry, requestId);
            DeleteRows(InspectionRequestConstants.TableIrProduct, requestId);
        }

        private void DeleteRows(string tableName, string requestId)
        {
            IQuery query = _entityManager.CreateQuery(tableName);
            query.AddEquals("REQUEST_ID", requestId);

            foreach (IEntity entity in _entityManager.Select(query))
            {
                _entityManager.Transaction.Remove(entity);
            }
        }

        private bool HasSnapshotRows(string requestId)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableIrLoginPlanEntry);
            query.AddEquals("REQUEST_ID", requestId);
            return _entityManager.Select(query).Count > 0;
        }

        private bool NeedsTemplateFieldBackfill(string requestId)
        {
            IQuery entryQuery = _entityManager.CreateQuery(InspectionRequestConstants.TableIrLoginPlanEntry);
            entryQuery.AddEquals("REQUEST_ID", requestId);

            foreach (IEntity entry in _entityManager.Select(entryQuery))
            {
                if (string.IsNullOrWhiteSpace(GetString(entry, "ENTITY_TEMPLATE_ID")))
                {
                    continue;
                }

                IQuery fieldQuery = _entityManager.CreateQuery(InspectionRequestConstants.TableIrLoginPlanField);
                fieldQuery.AddEquals("REQUEST_ID", requestId);
                fieldQuery.AddEquals("PARENT_ORDER_NUMBER", GetValue(entry, "ORDER_NUMBER"));
                if (_entityManager.Select(fieldQuery).Count == 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void CopyDataAssignment(string requestId, string loginPlanId, string loginPlanVersion)
        {
            IQuery entryQuery = _entityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanEntry);
            entryQuery.AddEquals("IDENTITY", loginPlanId);
            if (!string.IsNullOrWhiteSpace(loginPlanVersion))
            {
                entryQuery.AddEquals("VERSION", loginPlanVersion);
            }

            foreach (IEntity sourceEntry in _entityManager.Select(entryQuery))
            {
                object orderNumber = GetValue(sourceEntry, "ORDER_NUMBER");
                object parentOrderNumber = GetValue(sourceEntry, "PARENT_ENTRY_ORDER_NUMBER");
                IEntity targetEntry = _entityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanEntry);
                targetEntry.Set("REQUEST_ID", requestId);
                targetEntry.Set("ORDER_NUMBER", orderNumber);
                targetEntry.Set("PARENT_ORDER_NUMBER", parentOrderNumber);
                targetEntry.Set("SOURCE_LOGIN_PLAN_ID", loginPlanId);
                targetEntry.Set("SOURCE_LOGIN_PLAN_VERSION", GetValue(sourceEntry, "VERSION"));
                targetEntry.Set("SOURCE_ORDER_NUMBER", orderNumber);
                targetEntry.Set("TABLE_NAME", GetValue(sourceEntry, "TABLE_NAME"));
                targetEntry.Set("NODE_NAME", GetValue(sourceEntry, "NODE_NAME"));
                targetEntry.Set("LOGIN_WORKFLOW_ID", GetValue(sourceEntry, "LOGIN_WORKFLOW_ID"));
                targetEntry.Set("LOGIN_WORKFLOW_VERSION", GetValue(sourceEntry, "LOGIN_WORKFLOW_VERSION"));
                targetEntry.Set("USE_LAST_ACTIVE_LOGIN_WORKFLOW", GetValue(sourceEntry, "USE_LAST_ACTIVE_LOGIN_WORKFLOW"));
                targetEntry.Set("ENTITY_TEMPLATE_ID", GetValue(sourceEntry, "ENTITY_TEMPLATE_ID"));
                targetEntry.Set("ENTITY_TEMPLATE_VERSION", GetValue(sourceEntry, "ENTITY_TEMPLATE_VERSION"));
                targetEntry.Set("LOGIN_CONDITION", GetValue(sourceEntry, "LOGIN_CONDITION"));
                targetEntry.Set("COUNT_EXPRESSION", GetValue(sourceEntry, "COUNT_EXPRESSION"));
                targetEntry.Set("MODIFIABLE", true);
                targetEntry.Set("REMOVEFLAG", false);
                _entityManager.Transaction.Add(targetEntry);

                if (string.IsNullOrWhiteSpace(GetString(sourceEntry, "ENTITY_TEMPLATE_ID")))
                {
                    CopyEntryFields(requestId, loginPlanId, sourceEntry, orderNumber);
                }
                else
                {
                    CopyEntityTemplateFields(requestId, loginPlanId, sourceEntry, orderNumber);
                }

                CopyEntryTests(requestId, loginPlanId, sourceEntry, orderNumber);
            }
        }

        private void CopyEntryFields(string requestId, string loginPlanId, IEntity sourceEntry, object parentOrderNumber)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanField);
            query.AddEquals("IDENTITY", loginPlanId);
            query.AddEquals("VERSION", GetValue(sourceEntry, "VERSION"));
            query.AddEquals("PARENT_ORDER_NUMBER", parentOrderNumber);

            foreach (IEntity sourceField in _entityManager.Select(query))
            {
                IEntity targetField = _entityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanField);
                targetField.Set("REQUEST_ID", requestId);
                targetField.Set("PARENT_ORDER_NUMBER", parentOrderNumber);
                targetField.Set("ORDER_NUMBER", GetValue(sourceField, "ORDER_NUMBER"));
                targetField.Set("PROPERTY", GetValue(sourceField, "PROPERTY"));
                targetField.Set("VALUE", GetValue(sourceField, "VALUE"));
                targetField.Set("OVERRIDE_VALUE", GetValue(sourceField, "VALUE"));
                targetField.Set("MODIFIABLE", true);
                _entityManager.Transaction.Add(targetField);
            }
        }

        private void CopyEntityTemplateFields(string requestId, string loginPlanId, IEntity sourceEntry, object parentOrderNumber)
        {
            string entityTemplateId = GetString(sourceEntry, "ENTITY_TEMPLATE_ID");
            string entityTemplateVersion = GetString(sourceEntry, "ENTITY_TEMPLATE_VERSION");
            if (string.IsNullOrWhiteSpace(entityTemplateId))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(entityTemplateVersion))
            {
                entityTemplateVersion = ResolveEntityTemplateVersion(entityTemplateId);
            }

            IQuery propertyQuery = _entityManager.CreateQuery(InspectionRequestConstants.TableEntityTemplateProperty);
            propertyQuery.AddEquals("ENTITY_TEMPLATE_ID", entityTemplateId);
            if (!string.IsNullOrWhiteSpace(entityTemplateVersion))
            {
                propertyQuery.AddEquals("ENTITY_TEMPLATE_VERSION", entityTemplateVersion);
            }

            foreach (IEntity templateProperty in _entityManager.Select(propertyQuery))
            {
                string propertyName = GetString(templateProperty, "PROPERTY_NAME");
                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    continue;
                }

                object value = ResolveLoginPlanFieldValue(loginPlanId, sourceEntry, parentOrderNumber, propertyName)
                    ?? NormalizeTemplateValue(GetValue(templateProperty, "DEFAULT_VALUE"));

                IEntity targetField = _entityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanField);
                targetField.Set("REQUEST_ID", requestId);
                targetField.Set("PARENT_ORDER_NUMBER", parentOrderNumber);
                targetField.Set("ORDER_NUMBER", GetValue(templateProperty, "ORDER_NUM"));
                targetField.Set("PROPERTY", propertyName);
                targetField.Set("VALUE", value);
                targetField.Set("OVERRIDE_VALUE", value);
                targetField.Set("MODIFIABLE", true);
                _entityManager.Transaction.Add(targetField);
            }
        }

        private object ResolveLoginPlanFieldValue(string loginPlanId, IEntity sourceEntry, object parentOrderNumber, string propertyName)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanField);
            query.AddEquals("IDENTITY", loginPlanId);
            query.AddEquals("VERSION", GetValue(sourceEntry, "VERSION"));
            query.AddEquals("PARENT_ORDER_NUMBER", parentOrderNumber);
            query.AddEquals("PROPERTY", propertyName);

            foreach (IEntity sourceField in _entityManager.Select(query))
            {
                return GetValue(sourceField, "VALUE");
            }

            return null;
        }

        private string ResolveEntityTemplateVersion(string entityTemplateId)
        {
            IEntity entityTemplate = _entityManager.SelectLatestVersion(
                InspectionRequestConstants.TableEntityTemplate,
                entityTemplateId) as IEntity;

            return entityTemplate == null || !entityTemplate.IsValid()
                ? string.Empty
                : GetString(entityTemplate, "ENTITY_TEMPLATE_VERSION");
        }

        private void CopyEntryTests(string requestId, string loginPlanId, IEntity sourceEntry, object parentOrderNumber)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanTest);
            query.AddEquals("IDENTITY", loginPlanId);
            query.AddEquals("VERSION", GetValue(sourceEntry, "VERSION"));
            query.AddEquals("PARENT_ORDER_NUMBER", parentOrderNumber);

            foreach (IEntity sourceTest in _entityManager.Select(query))
            {
                object orderNumber = GetValue(sourceTest, "ORDER_NUMBER");
                IEntity targetTest = _entityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanTest);
                targetTest.Set("REQUEST_ID", requestId);
                targetTest.Set("PARENT_ORDER_NUMBER", parentOrderNumber);
                targetTest.Set("ORDER_NUMBER", orderNumber);
                targetTest.Set("ANALYSIS_IDENTITY", GetValue(sourceTest, "ANALYSIS_IDENTITY"));
                targetTest.Set("ANALYSIS_VERSION", GetValue(sourceTest, "ANALYSIS_VERSION"));
                targetTest.Set("COMP_LIST_ANALYSIS", GetValue(sourceTest, "COMP_LIST_ANALYSIS"));
                targetTest.Set("COMP_LIST_ANALYSIS_VERSION", GetValue(sourceTest, "COMP_LIST_ANALYSIS_VERSION"));
                targetTest.Set("COMP_LIST_IDENTITY", GetValue(sourceTest, "COMP_LIST_IDENTITY"));
                targetTest.Set("TEST_SCHEDULE_ID", GetValue(sourceTest, "TEST_SCHEDULE_ID"));
                targetTest.Set("RED_TEST_ID", GetValue(sourceTest, "RED_TEST_ID"));
                targetTest.Set("RED_TEST_VERSION", GetValue(sourceTest, "RED_TEST_VERSION"));
                targetTest.Set("LAST_ACTIVE_VERSION", GetValue(sourceTest, "LAST_ACTIVE_VERSION"));
                targetTest.Set("MODIFIABLE", true);
                _entityManager.Transaction.Add(targetTest);

                CopyTestFields(requestId, loginPlanId, sourceEntry, sourceTest, parentOrderNumber, orderNumber);
            }
        }

        private void CopyTestFields(string requestId, string loginPlanId, IEntity sourceEntry, IEntity sourceTest, object entryOrderNumber, object testOrderNumber)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableLoginPlanTestField);
            query.AddEquals("IDENTITY", loginPlanId);
            query.AddEquals("VERSION", GetValue(sourceEntry, "VERSION"));
            query.AddEquals("PARENT_PARENT_ORDER_NUMBER", entryOrderNumber);
            query.AddEquals("PARENT_ORDER_NUMBER", testOrderNumber);

            foreach (IEntity sourceField in _entityManager.Select(query))
            {
                IEntity targetField = _entityManager.CreateEntity(InspectionRequestConstants.TableIrLoginPlanTestField);
                targetField.Set("REQUEST_ID", requestId);
                targetField.Set("PARENT_PARENT_ORDER_NUMBER", entryOrderNumber);
                targetField.Set("PARENT_ORDER_NUMBER", testOrderNumber);
                targetField.Set("ORDER_NUMBER", GetValue(sourceField, "ORDER_NUMBER"));
                targetField.Set("PROPERTY", GetValue(sourceField, "PROPERTY"));
                targetField.Set("VALUE", GetValue(sourceField, "VALUE"));
                targetField.Set("OVERRIDE_VALUE", GetValue(sourceField, "VALUE"));
                targetField.Set("MODIFIABLE", true);
                _entityManager.Transaction.Add(targetField);
            }
        }

        private void CopyProductSpec(string requestId, string loginPlanId, string loginPlanVersion)
        {
            IQuery query = _entityManager.CreateQuery(InspectionRequestConstants.TableMlpHeader);
            query.AddEquals("LOGIN_PLAN_ID", loginPlanId);
            if (!string.IsNullOrWhiteSpace(loginPlanVersion))
            {
                query.AddEquals("LOGIN_PLAN_VERSION", loginPlanVersion);
            }

            int orderNumber = 1;
            foreach (IEntity product in _entityManager.Select(query))
            {
                IEntity targetProduct = _entityManager.CreateEntity(InspectionRequestConstants.TableIrProduct);
                targetProduct.Set("REQUEST_ID", requestId);
                targetProduct.Set("ORDER_NUMBER", orderNumber.ToString(CultureInfo.InvariantCulture));
                orderNumber++;
                targetProduct.Set("PRODUCT_ID", GetValue(product, "IDENTITY"));
                targetProduct.Set("PRODUCT_VERSION", GetValue(product, "PRODUCT_VERSION"));
                targetProduct.Set("PRODUCT_CODE", GetValue(product, "PRODUCT_CODE"));
                targetProduct.Set("PRODUCT_DESCRIPTION", GetValue(product, "DESCRIPTION"));
                targetProduct.Set("TEST_SCHEDULE_ID", GetValue(product, "TEST_SCHEDULE"));
                targetProduct.Set("INSPECTION_PLAN", GetValue(product, "INSPECTION_PLAN"));
                targetProduct.Set("MODIFIABLE", true);
                targetProduct.Set("REMOVEFLAG", false);
                _entityManager.Transaction.Add(targetProduct);
            }
        }

        private static object GetValue(IEntity entity, string fieldName)
        {
            return entity.Get(fieldName);
        }

        private static object NormalizeTemplateValue(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string text = value.ToString();
            return string.Equals(text, "T", StringComparison.OrdinalIgnoreCase)
                ? "True"
                : string.Equals(text, "F", StringComparison.OrdinalIgnoreCase)
                    ? "False"
                    : text;
        }

        private static string GetString(IEntity entity, string fieldName)
        {
            object value = entity.Get(fieldName);
            return value?.ToString() ?? string.Empty;
        }

    }
}
