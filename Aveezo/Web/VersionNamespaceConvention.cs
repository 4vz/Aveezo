using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Versioning.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aveezo
{
    public class VersionNamespaceConvention : IControllerConvention
    {
        #region Fields

        private string prefix = null;

        #endregion

        #region Constructors

        public VersionNamespaceConvention(string prefix)
        {
            this.prefix = prefix;
        }


        #endregion

        #region Methods

        public virtual bool Apply(IControllerConventionBuilder controller, ControllerModel controllerModel)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            if (controllerModel == null)
                throw new ArgumentNullException(nameof(controllerModel));

            var match = Regex.Match(controllerModel.ControllerType.Namespace!, @$"(?:(?:^{prefix}|\.{prefix})\.([A-Za-z]+)\.)?[vV](\d+)[\.$]?", RegexOptions.Singleline);

            var groups = new List<string>();
            var versions = new List<string>();

            while (match.Success)
            {
                var group1 = match.Groups[1];
                var group2 = match.Groups[2];

                match = match.NextMatch();

                if (group2.Success == false) continue;

                groups.Add($"{group1.Value}");
                versions.Add($"{group2.Value}");                
            }

            if (versions.Count > 1)
                throw new InvalidOperationException();
            else if (versions.Count == 1)
            {
                var group = groups[0].Trim().ToLower();
                if (group == "") group = "main";
                else group = group.Replace("v", "ooo"); // status cant use v character -bug

                var version = versions[0];

                if (int.TryParse(version, out int vers))
                {
                    var apiVersion = new ApiVersion(vers, 0, group);

                    if (controllerModel.Attributes.OfType<ObsoleteAttribute>().Any()) controller.HasDeprecatedApiVersion(apiVersion!);
                    else controller.HasApiVersion(apiVersion!);

                    return true;
                }
                else return false;
            }
            else return false;
        }

        #endregion
    }
}
