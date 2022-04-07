﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.DiaResolver;
using GoogleTestAdapter.Model;

namespace GoogleTestAdapter.TestCases
{

    internal class TestCaseResolver
    {
        // see GTA_Traits.h
        private const string TraitAppendix = "_GTA_TRAIT";

        private readonly IDiaResolverFactory _diaResolverFactory;
        private readonly ILogger _logger;

        internal TestCaseResolver(IDiaResolverFactory diaResolverFactory, ILogger logger)
        {
            _diaResolverFactory = diaResolverFactory;
            _logger = logger;
        }

        internal List<TestCaseLocation> ResolveAllTestCases(string executable, List<string> testMethodSignatures, string symbolFilterString, string pathExtension)
        {
            List<TestCaseLocation> testCaseLocationsFound =
                FindTestCaseLocationsInBinary(executable, testMethodSignatures, symbolFilterString, pathExtension).ToList();

            if (testCaseLocationsFound.Count == 0)
            {
                List<string> imports = PeParser.ParseImports(executable, _logger);

                string moduleDirectory = Path.GetDirectoryName(executable);

                foreach (string import in imports)
                {
                    // ReSharper disable once AssignNullToNotNullAttribute
                    string importedBinary = Path.Combine(moduleDirectory, import);
                    if (File.Exists(importedBinary))
                    {
                        testCaseLocationsFound.AddRange(FindTestCaseLocationsInBinary(importedBinary, testMethodSignatures, symbolFilterString, pathExtension));
                    }
                }
            }
            return testCaseLocationsFound;
        }


        private IEnumerable<TestCaseLocation> FindTestCaseLocationsInBinary(
            string binary, List<string> testMethodSignatures, string symbolFilterString, string pathExtension)
        {
            using (IDiaResolver diaResolver = _diaResolverFactory.Create(binary, pathExtension, _logger))
            {
                try
                {
                    IList<SourceFileLocation> allTestMethodSymbols = diaResolver.GetFunctions(symbolFilterString);
                    IList<SourceFileLocation> allTraitSymbols = diaResolver.GetFunctions("*" + TraitAppendix);
                    _logger.DebugInfo($"Found {allTestMethodSymbols.Count} test method symbols and {allTraitSymbols.Count} trait symbols in binary {binary}");

                    return allTestMethodSymbols
                        .Where(nsfl => testMethodSignatures.Any(tms => Regex.IsMatch(nsfl.Symbol, tms))) // Contains() instead of == because nsfl might contain namespace
                        .Select(nsfl => ToTestCaseLocation(nsfl, allTraitSymbols))
                        .ToList(); // we need to force immediate query execution, otherwise our session object will already be released
                }
                catch (Exception e)
                {
                    _logger.DebugError($"Exception while resolving test locations and traits in {binary}\n{e}");
                    return new TestCaseLocation[0];
                }
            }
        }

        private TestCaseLocation ToTestCaseLocation(SourceFileLocation location, IEnumerable<SourceFileLocation> allTraitSymbols)
        {
            List<Trait> traits = NewTestCaseResolver.GetTraits(location, allTraitSymbols);
            var testCaseLocation = new TestCaseLocation(location.Symbol, location.Sourcefile, location.Line);
            testCaseLocation.Traits.AddRange(traits);
            return testCaseLocation;
        }

    }

}