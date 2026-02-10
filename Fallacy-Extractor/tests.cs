using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fallacy_Extractor;
using NUnit.Framework;
using SharpYaml;
using SharpYaml.Serialization;

[TestFixture]
public class FallacyTests
{
    private NLangParser parser;
    private Serializer serializer;
    

    [SetUp]
    public void Setup()
    {
        TestContext.Out.WriteLine("hello");
        parser = new NLangParser();
        serializer = new Serializer();
    }
    [Test]
    public void ThisTestMustFail()
    {
        Assert.Fail("If you see this, tests ARE running.");
    }


    private string GetTestYaml()
    {
        return @"Version: ""1.0""
Document:
  ID: ""doc-uuid""
  Source: ""input_text""
  Language: ""da""
  PageCount: 1
Nodes:
  - ID: ""p1""
    Role: ""Premise""
    Explicit: true
    Text: > 
      what is love?
    Confidence: 0.95
    TextSpan:
      Page: 1
      Start: 0
      End: 13
    InferredFrom: []
  - ID: ""ip1""
    Role: ""Premise""
    Explicit: false
    Text: >
      The existence of love is questioned.
    Confidence: 0.75
    TextSpan: null
    InferredFrom: [""p1"", ""p2""]
Edges:
  - ID: ""e1""
    From: ""p1""
    To: ""ip1""
    Relation: ""Supports""
    Confidence: 0.85
Fallacies: []
Meta:
  Warnings: []
  Stats:
    NodeCount: 2
    EdgeCount: 1
    ImplicitPremisesAmount: 1";
    }

    [Test]
    public void TestYamlDeserializationAndValidation()
    {
        TestContext.WriteLine("hello");
        var yamlString = GetTestYaml();

        if ((int)yamlString[0] == 96)
            yamlString = NLangParser.DeCodeBlock(yamlString);

        Root root = null;

        Assert.DoesNotThrow(() =>
        {
            root = serializer.Deserialize<Root>(yamlString);
        });

        Assert.That(root, Is.Not.Null, "YAML should deserialize into a Root object");

        var errors = YAMLer.Validate(root!);
        Assert.That(errors, Is.Empty,
            "YAML validation errors found: " + string.Join(", ", errors));
    }


    [Test]
    public async Task TestFallacyDetectionPass()
    {
        TestContext.WriteLine("hello");
        var yamlString = GetTestYaml();
        var root = serializer.Deserialize<Root>(yamlString);

        Assert.That(root, Is.Not.Null, "YAML should deserialize into a Root object");

        List<Fallacy> fallacies = null;

        Assert.DoesNotThrowAsync(async () =>
        {
            fallacies = await parser.FallacyDetect(root!);
        });

        Assert.That(fallacies, Is.Not.Null, "FallacyDetect should return a list");

        foreach (var f in fallacies!)
        {
            Assert.That(f.Description, Is.Not.Empty,
                $"Fallacy {f.ID} should have a description");

            Assert.That(f.Confidence, Is.InRange(0.0, 1.0),
                $"Fallacy {f.ID} confidence out of range");
        }
    }

}
