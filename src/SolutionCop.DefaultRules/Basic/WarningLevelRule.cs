﻿namespace SolutionCop.DefaultRules.Basic
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Core;
    using Properties;

    [Export(typeof(IProjectRule))]
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1121:UseBuiltInTypeAlias", Justification = "OK here")]
    public class WarningLevelRule : ProjectRule<Tuple<int, IDictionary<string, int>>>
    {
        public override string Id
        {
            get { return "WarningLevel"; }
        }

        public override XElement DefaultConfig
        {
            get
            {
                var element = new XElement(Id);
                element.SetAttributeValue("enabled", "false");
                element.Add(new XElement("MinimalValue", "4"));
                element.Add(new XElement("Exception", new XElement("Project", "ProjectThatIsAllowedToHaveWarningLevel_2.csproj"), new XElement("MinimalValue", "2")));
                element.Add(new XElement("Exception", new XElement("Project", "AnotherProjectToFullyExcludeFromChecks.csproj")));
                return element;
            }
        }

        protected override Tuple<int, IDictionary<string, int>> ParseConfigurationSection(XElement xmlRuleConfigs, List<string> errors)
        {
            ValidateConfigSectionForAllowedElements(xmlRuleConfigs, errors, "Exception", "MinimalValue");
            var xmlMinimalValue = xmlRuleConfigs.Element("MinimalValue");
            int requiredWarningLevel = 4;
            if (xmlMinimalValue == null)
            {
                errors.Add($"Bad configuration for rule {Id}: <MinimalValue> element is missing.");
            }
            else if (!Int32.TryParse((string)xmlMinimalValue, out requiredWarningLevel))
            {
                errors.Add(string.Format(Resources.BadConfiguration, Id, "<MinimalValue> element must contain an integer."));
            }
            var exceptions = new Dictionary<string, int>();
            foreach (var xmlException in xmlRuleConfigs.Elements("Exception"))
            {
                var xmlProject = xmlException.Element("Project");
                if (xmlProject == null)
                {
                    errors.Add($"Bad configuration for rule {Id}: <Project> element is missing in exceptions list.");
                }
                else
                {
                    xmlMinimalValue = xmlException.Element("MinimalValue");
                    var minimalValue = xmlMinimalValue == null ? 0 : Convert.ToInt32(xmlMinimalValue.Value.Trim());
                    exceptions.Add(xmlProject.Value, minimalValue);
                }
            }
            return Tuple.Create<int, IDictionary<string, int>>(requiredWarningLevel, exceptions);
        }

        protected override IEnumerable<string> ValidateSingleProject(XDocument xmlProject, string projectFilePath, Tuple<int, IDictionary<string, int>> ruleConfiguration)
        {
            var exceptions = ruleConfiguration.Item2;
            var projectFileName = Path.GetFileName(projectFilePath);
            int requiredWarningLevel;
            if (exceptions.TryGetValue(projectFileName, out requiredWarningLevel))
            {
                Console.Out.WriteLine("DEBUG: Project has exceptional warning level {0}: {1}", requiredWarningLevel, projectFileName);
            }
            else
            {
                requiredWarningLevel = ruleConfiguration.Item1;
                Console.Out.WriteLine("DEBUG: Project has standard warning level {0}: {1}", requiredWarningLevel, projectFileName);
            }
            var xmlGlobalPropertyGroups = xmlProject.Descendants(Namespace + "PropertyGroup").Where(x => x.Attribute("Condition") == null);
            foreach (var xmlPropertyGroup in xmlGlobalPropertyGroups)
            {
                var xmlWarningLevel = xmlPropertyGroup.Descendants(Namespace + "WarningLevel").FirstOrDefault();
                var warningLevelInProject = xmlWarningLevel == null ? 1 : Int32.Parse(xmlWarningLevel.Value);
                if (warningLevelInProject >= requiredWarningLevel)
                {
                    Console.Out.WriteLine("DEBUG: Project has acceptable warning level in global section {0}: {1}", warningLevelInProject, projectFileName);
                    yield break;
                }
            }
            var xmlPropertyGroupsWithConditions = xmlProject.Descendants(Namespace + "PropertyGroup").Where(x => x.Attribute("Condition") != null);
            foreach (var xmlPropertyGroupsWithCondition in xmlPropertyGroupsWithConditions)
            {
                var xmlWarningLevel = xmlPropertyGroupsWithCondition.Descendants(Namespace + "WarningLevel").FirstOrDefault();
                var warningLevelInProject = xmlWarningLevel == null ? 0 : Int32.Parse(xmlWarningLevel.Value);
                if (warningLevelInProject < requiredWarningLevel)
                {
                    yield return $"Warning level {warningLevelInProject} is lower than required {requiredWarningLevel} in project {projectFileName}. Please make sure that setting is active for ALL configurations.";
                    yield break;
                }
            }
        }
    }
}