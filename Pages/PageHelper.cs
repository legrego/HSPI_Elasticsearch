﻿using HomeSeerAPI;
using Hspi.Properties;
using NullGuard;
using Scheduler;
using System;
using System.Collections.Specialized;
using System.Text;
using System.Web;

namespace Hspi.Pages
{
    [NullGuard(ValidationFlags.Arguments | ValidationFlags.NonPublic)]
    internal abstract class PageHelper : PageBuilderAndMenu.clsPageBuilder
    {
        protected PageHelper(IHSApplication HS, PluginConfig pluginConfig, string pageName) : base(pageName)
        {
            this.HS = HS;
            this.pluginConfig = pluginConfig;
            this.pageName = pageName;
        }

        public static string HtmlEncode<T>([AllowNull]T value) => value == null ? string.Empty : HttpUtility.HtmlEncode(value);

        protected string FormDropDown(string name, NameValueCollection options, string selected = "", int width = 50, bool autoPostBack = false)
        {
            var dropdown = new clsJQuery.jqDropList(name, pageName, false)
            {
                selectedItemIndex = -1,
                id = NameToIdWithPrefix(name),
                autoPostBack = autoPostBack,
                //toolTip = tooltip,
                //style = $"width: {width}px;",
                enabled = true,
                submitForm = autoPostBack
            };

            if (options != null)
            {
                for (var i = 0; i < options.Count; i++)
                {
                    var sel = options.GetKey(i) == selected;
                    dropdown.AddItem(options.Get(i), options.GetKey(i), sel);
                }
            }

            return dropdown.Build();
        }

        protected static string HtmlTextBox(string name, [AllowNull]string defaultText, int size = 25, string type = "text", bool @readonly = false)
        {
            return $"<input type=\'{type}\' id=\'{NameToIdWithPrefix(name)}\' size=\'{size}\' name=\'{name}\' value=\'{HtmlEncode(defaultText)}\' {(@readonly ? "readonly" : string.Empty)}>";
        }

        protected static string NameToIdWithPrefix(string name)
        {
            return $"{ IdPrefix}{NameToId(name)}";
        }

        protected static string TextArea(string name, [AllowNull]string defaultText, int rows = 6, int cols = 120, bool @readonly = false)
        {
            return $"<textarea form_id=\'{NameToIdWithPrefix(name)}\' rows=\'{rows}\' cols=\'{cols}\' name=\'{name}\'  {(@readonly ? "readonly" : string.Empty)}>{HtmlEncode(defaultText)}</textarea>";
        }

        protected string FormButton(string name, string label, string toolTip, bool autoPostBack = true)
        {
            var button = new clsJQuery.jqButton(name, label, PageName, autoPostBack)
            {
                id = NameToIdWithPrefix(name),
                toolTip = toolTip
            };
            button.toolTip = toolTip;
            button.enabled = true;

            return button.Build();
        }

        protected string FormCheckBox(string name, string label, bool @checked, bool autoPostBack = false)
        {
            UsesjqCheckBox = true;
            var cb = new clsJQuery.jqCheckBox(name, label, PageName, true, true)
            {
                id = NameToIdWithPrefix(name),
                @checked = @checked,
                autoPostBack = autoPostBack
            };
            return cb.Build();
        }

        protected string FormPageButton(string name, string label)
        {
            var b = new clsJQuery.jqButton(name, label, PageName, true)
            {
                id = NameToIdWithPrefix(name)
            };

            return b.Build();
        }

        protected string FormTextBox(string name, string label, [AllowNull]string defaultText,
                                     int size = 50, string type = "text", bool autoSubmit = true)
        {
            var b = new clsJQuery.jqTextBox(name, type, defaultText ?? string.Empty, PageName, size, autoSubmit)
            {
                id = NameToIdWithPrefix(name),
                label = label
            };

            return b.Build();
        }

        protected string FormTimeSpan(string name, string label, TimeSpan timeSpan, bool submit)
        {
            var b = new clsJQuery.jqTimeSpanPicker(name, label, PageName, submit)
            {
                id = NameToIdWithPrefix(name),
                defaultTimeSpan = timeSpan
            };

            return b.Build();
        }

        protected void IncludeResourceCSS(StringBuilder stb, string scriptFile)
        {
            stb.AppendLine("<style type=\"text/css\">");
            stb.AppendLine(Resources.ResourceManager.GetString(scriptFile.Replace('.', '_'), Resources.Culture));
            stb.AppendLine("</style>");
            AddScript(stb.ToString());
        }

        protected void IncludeResourceScript(StringBuilder stb, string scriptFile, string uniqueControlId = "")
        {
            string script = Resources.ResourceManager.GetString(scriptFile.Replace('.', '_'), Resources.Culture);
            script = script.Replace("__uniqueControlId__", uniqueControlId);

            stb.AppendLine("<script type='text/javascript'>");
            stb.AppendLine(script);
            stb.AppendLine("</script>");
            AddScript(stb.ToString());
        }

        private static string NameToId(string name)
        {
            return name.Replace(' ', '_');
        }

        protected string pageName;
        protected const string PageTypeId = "pagetype";
        protected const string RecordId = "recordid";
        protected readonly IHSApplication HS;
        protected readonly PluginConfig pluginConfig;
        private const string IdPrefix = "id_";
    }
}