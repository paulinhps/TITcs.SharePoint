﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Client;
using TITcs.SharePoint.Log;
using DraftVisibilityType = Microsoft.SharePoint.DraftVisibilityType;

namespace TITcs.SharePoint.Utils
{
    public class ListUtils
    {
        /// <summary>
        /// Enable Anonymous Access list
        /// </summary>
        /// <param name="web">Context</param>
        /// <param name="listTitle">List title</param>
        public static void EnableAccessAnonymous(SPWeb web, string listTitle)
        {
            EnableAccessAnonymous(web, listTitle);
        }

        public static void EnableAccessAnonymous(SPWeb web, string listTitle, SPBasePermissions basePermissions = SPBasePermissions.ViewPages |
                    SPBasePermissions.OpenItems | SPBasePermissions.ViewVersions |
                    SPBasePermissions.Open | SPBasePermissions.UseClientIntegration |
                    SPBasePermissions.ViewFormPages | SPBasePermissions.ViewListItems)
        {
            runCodeInListInstance(web, listTitle, (list) =>
            {
                list.BreakRoleInheritance(true, false);
                list.AllowEveryoneViewItems = true;
                list.AnonymousPermMask64 = basePermissions;

                list.Update();

                Logger.Information("ListUtils.EnableAccessAnonymous", "Anonymous access enabled in the \"{0}\"", listTitle);
            });
            
        }

        /// <summary>
        /// Disable Anonymous Access list
        /// </summary>
        /// <param name="web">Context</param>
        /// <param name="listTitle">List title</param>
        public static void DisableAccessAnonymous(SPWeb web, string listTitle)
        {
            runCodeInListInstance(web, listTitle, (list) =>
            {
                list.ResetRoleInheritance();
                list.Update();
            });

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="web"></param>
        /// <param name="listTitle"></param>
        /// <param name="fieldName"></param>
        /// <param name="allow"></param>
        /// <reference>https://msdn.microsoft.com/en-us/library/office/ee536168(v=office.14).aspx</reference>
        public static void AllowDuplicateValues(SPWeb web, string listTitle, string fieldName, bool allow = true)
        {
            runCodeInListInstance(web, listTitle, (list) =>
            {
                if (list.ItemCount > 0 && !allow)
                {
                    Logger.Unexpected("ListUtils.AllowDuplicateValues", "Could not allow duplicate values for the field \"{0}\" list \"{1}\" because it contains items. Remove all items from the list.", fieldName, listTitle);
                    return;
                }

                if (list.Fields.ContainsField(fieldName))
                {
                    SPField field = list.Fields[fieldName];

                    field.Indexed = !allow;
                    //field.AllowDuplicateValues = false;
                    field.EnforceUniqueValues = !allow;

                    field.Update();
                    list.Update();

                    var message = "The \"{0}\" field no longer allows duplicate values";

                    if (!allow)
                        message = "The \"{0}\" field allows duplicate values";

                    Logger.Information("ListUtils.AllowDuplicateValues", message, fieldName);
                }
            });
        }

        public static void ChangeDisplayNameInField(SPWeb web, string listTitle, string fieldName, string displayName)
        {
            runCodeInListInstance(web, listTitle, (list) =>
            {
                if (list.Fields.ContainsField(fieldName))
                {
                    SPField field = list.Fields[fieldName];

                    field.Title = displayName;

                    field.Update();
                    list.Update();

                    Logger.Information("ListUtils.ChangeDisplayNameInField", "The display name of the \"{0}\" list \"{1}\" was changed to \"{2}\"", listTitle, fieldName, displayName);
                }
            });
        }

        public static void ChangeTitle(SPWeb web, string listTitle, string title)
        {
            runCodeInListInstance(web, listTitle, (list) =>
            {

                list.Title = title;
                list.Update();

                Logger.Information("ListUtils.ChangeTitle", "The title of the list \"{0}\" has changed to \"{1}\"", listTitle, title);

            });
        }

        public static void DraftVersionVisibility(SPWeb web, string listTitle, DraftVisibilityType draftVisibilityType = DraftVisibilityType.Reader)
        {
            runCodeInListInstance(web, listTitle, (list) =>
            {

                list.DraftVersionVisibility = draftVisibilityType;
                list.Update();

                if(draftVisibilityType == DraftVisibilityType.Reader)
                    Logger.Information("ListUtils.ChangeTitle", "The list \"{0}\" has setted to \"Any user who can read items\"", listTitle);
                else if (draftVisibilityType == DraftVisibilityType.Author)
                    Logger.Information("ListUtils.ChangeTitle", "The list \"{0}\" has setted to \"Only users who can edit items\"", listTitle);
                else if (draftVisibilityType == DraftVisibilityType.Approver)
                    Logger.Information("ListUtils.ChangeTitle", "The list \"{0}\" has setted to \"Only users who can approve items (and the author of the item)\"", listTitle);

            });
        }

        private static void runCodeInListInstance(SPWeb web, string listTitle, Action<SPList> action)
        {
            var list = web.Lists.TryGetList(listTitle);

            if (list != null)
            {
                var allowSafeUpdates = web.AllowUnsafeUpdates;
                web.AllowUnsafeUpdates = true;

                action(list);

                web.AllowUnsafeUpdates = allowSafeUpdates;
            }
            else
                Logger.Unexpected("ListUtils.runCodeInListInstance", "The list \"{0}\" does not exist", listTitle);
        }

        private static void runCodeInListInstance(ClientContext clientContext, string listTitle, Action<List> action)
        {
            Web web = clientContext.Web;
            ListCollection lists = web.Lists;

            IEnumerable<List> existingLists = clientContext.LoadQuery(lists.Where(list => list.Title == listTitle));

            clientContext.ExecuteQuery();

            var existingList = existingLists.FirstOrDefault();

            if (existingList != null)
            {
                action(existingList);
            }
            else
                Logger.Unexpected("ListUtils.runCodeInListInstance", "The list \"{0}\" does not exist", listTitle);
        }

        public static void AddField(SPWeb web, string listTitle, string internalNameOfField, string displayNameOfField, bool isViewField, bool showEditCreateForm, bool showDisplayForm)
        {
            runCodeInListInstance(web, listTitle, (list) =>
            {
                if (!list.Fields.ContainsField(displayNameOfField) &&
                    web.AvailableFields.ContainsField(internalNameOfField))
                {
                    if (!list.Fields.ContainsFieldWithStaticName(internalNameOfField))
                    {
                        SPField field = null;

                        try
                        {
                            field = web.AvailableFields[displayNameOfField];
                        }
                        catch (Exception)
                        {
                            field = web.AvailableFields.GetFieldByInternalName(internalNameOfField);
                        }

                        field.ShowInDisplayForm = showDisplayForm;
                        field.ShowInViewForms = showDisplayForm;
                        field.ShowInEditForm = showEditCreateForm;
                        field.ShowInNewForm = showEditCreateForm;
                        list.Fields.Add(field);

                        if (isViewField)
                        {
                            SPView viewList = list.DefaultView;
                            viewList.ViewFields.Add(web.AvailableFields[displayNameOfField]);
                            viewList.ViewFields.MoveFieldTo(internalNameOfField, 2);
                            viewList.Update();
                        }
                    }
                    else
                    {
                        var field = list.Fields.GetFieldByInternalName(internalNameOfField);

                        if (field.Title.ToLower() != displayNameOfField.ToLower())
                        {
                            field.Title = displayNameOfField;
                            field.Update();
                        }
                    }

                    Logger.Information("ListUtils.AddField", "The field \"{0}\" was added to the list \"{1}\"", displayNameOfField, listTitle);

                    list.Update();
                }
                else
                {
                    Logger.Unexpected("ListUtils.AddField", string.Format("There is already a field in the list \"{2}\" with the name \"{0}\" and internal name \"{1}\"", displayNameOfField, internalNameOfField, listTitle));
                }
            });
        }
    }
}
