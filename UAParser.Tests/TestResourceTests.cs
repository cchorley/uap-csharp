using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using YamlDotNet.RepresentationModel;

namespace UAParser.Tests
{
    public class TestResourceTests
    {
        [Fact]
        public async Task can_run_device_tests()
        {
            await RunTests("UAParser.Tests.TestResources.test_device.yaml", DeviceYamlTestCase.ReadFromMap);
        }

        [Fact]
        public async Task can_run_additional_os_tests()
        {
            await RunTests("UAParser.Tests.TestResources.additional_os_tests.yaml", OSYamlTestCase.ReadFromMap);
        }

        [Fact]
        public async Task can_run_firefox_user_agent_string_tests()
        {
            await RunTests("UAParser.Tests.TestResources.firefox_user_agent_strings.yaml", UserAgentYamlTestCase.ReadFromMap);
        }

        [Fact]
        public async Task can_run_pgts_browser_list_tests()
        {
            await RunTests("UAParser.Tests.TestResources.pgts_browser_list.yaml", UserAgentYamlTestCase.ReadFromMap);
        }

        [Fact]
        public async Task can_run_user_agent_parser_tests()
        {
            await RunTests("UAParser.Tests.TestResources.test_ua.yaml", UserAgentYamlTestCase.ReadFromMap);
        }

        [Fact]
        public async Task can_run_user_agent_parser_os_tests()
        {
            await RunTests("UAParser.Tests.TestResources.test_os.yaml", OSYamlTestCase.ReadFromMap);
        }

        internal async Task RunTests<TTestCase>(
          string resourceName,
          Func<Dictionary<string, string>, TTestCase> testCaseFunction) where TTestCase : YamlTestCase
        {
            List<TTestCase> testCases = GetTestCases(
              resourceName,
              "test_cases",
              testCaseFunction);

            await RunTestCases(testCases);
        }

        private static async Task RunTestCases<TTestCase>(List<TTestCase> testCases) where TTestCase : YamlTestCase
        {
            Parser parser = Parser.GetDefault();
            Assert.NotEmpty(testCases);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < testCases.Count; i++)
            {
                var tc = testCases[i];
                if (tc == null)
                    continue;

                var clientInfo = await parser.ParseAsync(tc.UserAgent);
                try
                {
                    tc.Verify(clientInfo);
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"test case {(i + 1)}: {ex.Message}");
                }
            }
            Assert.True(0 == sb.Length, "Failed tests: " + Environment.NewLine + sb);
        }

        public List<TTestCase> GetTestCases<TTestCase>(
      string resourceName,
      string yamlNodeName,
      Func<Dictionary<string, string>, TTestCase> testCaseFunction)
        {
            string yamlContent = this.GetTestResources(resourceName);
            YamlStream yaml = new YamlStream();
            yaml.Load(new StringReader(yamlContent));

            //reading overall configurations
            var regexConfigNode = (YamlMappingNode)yaml.Documents[0].RootNode;
            var regexConfig = new Dictionary<string, YamlNode>();
            foreach (var entry in regexConfigNode.Children)
            {
                regexConfig.Add(((YamlScalarNode)entry.Key).Value, entry.Value);
            }

            var testCasesNode = (YamlSequenceNode)regexConfig[yamlNodeName];
            List<TTestCase> testCases = testCasesNode.ConvertToDictionaryList()
              .Select(configMap =>
              {
                  if (!configMap.ContainsKey("js_ua")) //we deliberately skip tests with js-user agents
                return testCaseFunction(configMap);
                  return default(TTestCase);
              })
              .ToList();
            return testCases;
        }
    }
}
