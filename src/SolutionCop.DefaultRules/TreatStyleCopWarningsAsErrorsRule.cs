﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SolutionCop.Core;

namespace SolutionCop.DefaultRules
{
    [Export(typeof(IProjectRule))]
    public class TreatStyleCopWarningsAsErrorsRule : ProjectRule<string[]>
    {
        public override string Id
        {
            get { return "TreatStyleCopWarningsAsErrors"; }
        }

        public override XElement DefaultConfig
        {
            get
            {
                var element = new XElement(Id);
                element.SetAttributeValue("enabled", "false");
                element.Add(new XElement("Exception", new XElement("Project", "ProjectToExcludeFromCheck.csproj")));
                return element;
            }
        }

        protected override string[] ParseConfigurationSection(XElement xmlRuleConfigs, List<string> errors)
        {
            var unknownElements = xmlRuleConfigs.Elements().Select(x => x.Name.LocalName).Where(x => x != "Exception").ToArray();
            if (unknownElements.Any())
            {
                errors.Add(string.Format("Bad configuration for rule {0}: Unknown element(s) {1} in configuration.", Id, string.Join(",", unknownElements)));
            }
            foreach (var xmlException in xmlRuleConfigs.Elements("Exception"))
            {
                var xmlProject = xmlException.Element("Project");
                if (xmlProject == null)
                {
                    errors.Add(string.Format("Bad configuration for rule {0}: <Project> element is missing in exceptions list.", Id));
                }
            }
            return xmlRuleConfigs.Elements("Exception").Select(x => x.Value.Trim()).ToArray();
        }

        protected override IEnumerable<string> ValidateSingleProject(XDocument xmlProject, string projectFilePath, string[] exceptions)
        {
            var projectFileName = Path.GetFileName(projectFilePath);
            if (exceptions.Contains(projectFileName))
            {
                Console.Out.WriteLine("DEBUG: Skipping project with disabled StyleCop warnings as an exception: {0}", projectFileName);
            }
            else
            {
                var xmlPropertyGlobalGroups = xmlProject.Descendants(Namespace + "PropertyGroup").Where(x => x.Attribute("Condition") == null);
                var xmlPropertyGroupsWithConditions = xmlProject.Descendants(Namespace + "PropertyGroup").Where(x => x.Attribute("Condition") != null);
                foreach (var xmlPropertyGroupWithCondition in xmlPropertyGroupsWithConditions)
                {
                    var xmlTreatWarningsAsErrors = xmlPropertyGroupWithCondition.Descendants(Namespace + "StyleCopTreatErrorsAsWarnings").Concat(xmlPropertyGlobalGroups.Descendants(Namespace + "StyleCopTreatErrorsAsWarnings")).FirstOrDefault();
                    if (xmlTreatWarningsAsErrors != null)
                    {
                        if (xmlTreatWarningsAsErrors.Value == "true")
                        {
                            continue;
                        }
                    }
                    yield return string.Format("StyleCop warnings are not treated as errors in project {0}", projectFileName);
                }
            }
        }
    }
}