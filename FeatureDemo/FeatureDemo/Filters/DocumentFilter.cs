using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.FeatureManagement.Mvc;

namespace FeatureDemo.Filters
{
    public class DocumentFilter : IDocumentFilter
    {
        private readonly IConfiguration _config;
        private readonly IEnumerable<string> _enabledFeatures;

        public DocumentFilter(IConfiguration config)
        {
            _config = config;
            _enabledFeatures = GetEnabledFeatures();
        }
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach(var contextApiDescription in context.ApiDescriptions)
            {
                var actionDescriptor = (ControllerActionDescriptor)contextApiDescription.ActionDescriptor;

                var featureGateAttributes = actionDescriptor.MethodInfo.GetCustomAttributes<FeatureGateAttribute>();

                if (!featureGateAttributes.Any()) continue;

                if (IsFeatureEnabled(featureGateAttributes)) continue;

                var key = "/" + contextApiDescription.RelativePath.TrimEnd('/');
                swaggerDoc.Paths.Remove(key);
            }
        }

        private bool IsFeatureEnabled(IEnumerable<FeatureGateAttribute> featureGateAttributes)
        {
            foreach (var attribute in featureGateAttributes)
            {
                switch (attribute.RequirementType)
                {
                    case RequirementType.Any when attribute.Features.Any(x => _enabledFeatures.Contains(x)):
                        continue;
                    case RequirementType.Any:
                        return false;
                    case RequirementType.All when attribute.Features.All(x => _enabledFeatures.Contains(x)):
                        continue;
                    case RequirementType.All:
                        return false;
                }
            }

            return true;
        }

        private IEnumerable<string> GetEnabledFeatures()
        {
            foreach (var feature in _config.GetSection("FeatureManagement").GetChildren())
            {
                if (bool.TryParse(feature.Value, out bool featureValue) && featureValue)
                    yield return feature.Key;
            }
        }
    }
}