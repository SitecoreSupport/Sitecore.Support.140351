using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.ExperienceExplorer.Business.Constants;
using Sitecore.ExperienceExplorer.Business.Helpers;
using Sitecore.ExperienceExplorer.Business.Managers;
using Sitecore.ExperienceExplorer.Business.Utilities;
using Sitecore.ExperienceExplorer.Business.WebControls;
using Sitecore.Pipelines.RenderLayout;
using Sitecore.Publishing;
using Sitecore.Web;
using System;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Sitecore.Sites;

namespace Sitecore.Support.ExperienceExplorer.Business.Pipelines.RenderLayout
{
    public class InjectExperienceExplorerControlPipeline
    {
        private void EnsureFirstLoad()
        {
            if (HttpContext.Current.Session["IsFirstTime"] == null && !UserHelper.IsVirtualUser(Context.User.Name))
            {
                HttpContext.Current.Session["IsFirstTime"] = true;
                return;
            }
            HttpContext.Current.Session["IsFirstTime"] = false;
        }

        public void Process(RenderLayoutArgs args)
        {
            if (SettingsHelper.ExperienceModePipelineEnabled && Context.Item != null)
            {
                bool isExperienceMode = PageModeHelper.IsExperienceMode;
                if (isExperienceMode && !ExperienceExplorerUtil.CurrentTicketIsValid())
                {
                    PageModeHelper.RedirectToLoginPage();
                }
                if (!Context.IsLoggedIn)
                {
                    PreviewManager.RestoreUser();
                }
                if (!Context.IsLoggedIn && WebUtil.GetQueryStringOrCookie(SettingsHelper.AddOnQueryStringKey) == "1")
                {
                    WebUtil.Redirect(Factory.GetSite("login").VirtualFolder);
                    return;
                }
                if (SettingsHelper.IsEnabledForCurrentSite && Context.Site.DisplayMode == DisplayMode.Normal)
                {
                    bool isExpMode = PageModeHelper.IsExperienceMode;
                    try
                    {
                        this.SetIfNot(isExperienceMode ? "1" : "0");
                        if (isExperienceMode)
                        {
                            SettingsHelper.ExplorerWasAccessed=(true);
                            Control control = WebUtil.FindControlOfType(Context.Page.Page, typeof(HtmlForm));
                            if (control == null)
                            {
                                return;
                            }
                            Control child = Context.Page.Page.LoadControl(Paths.Module.Controls.GlobalHeaderPath);
                            control.Controls.AddAt(0, child);
                            Sitecore.ExperienceExplorer.Business.WebControls.ExperienceExplorer child2 = new Sitecore.ExperienceExplorer.Business.WebControls.ExperienceExplorer();
                            control.Controls.Add(child2);
                            ModuleManager.IsExpButtonClicked=(false);
                            HttpContext.Current.Items["IsExperienceMode"] = null;
                            this.EnsureFirstLoad();
                            if (!UserHelper.IsVirtualUser(Context.User.Name))
                            {
                                UserHelper.AuthentificateVirtualUser(Context.User.Name);
                            }
                        }
                        if (Context.PageMode.IsPreview || Context.PageMode.IsExperienceEditor || Context.PageMode.IsDebugging)
                        {
                            UserHelper.AuthentificateRealUser();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!ex.Message.Contains("code blocks"))
                        {
                            Log.Error("Inject Experience Explorer Control: ", ex, this);
                        }
                    }
                }
            }
        }

        protected virtual void SetIfNot(string value)
        {
            if (WebUtil.GetCookieValue(SettingsHelper.AddOnQueryStringKey) != value)
            {
                WebUtil.SetCookieValue(SettingsHelper.AddOnQueryStringKey, value);
            }
        }
    }
}
