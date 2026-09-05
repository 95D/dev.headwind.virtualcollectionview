using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Headwind.VirtualCollectionView.Editor
{
    /// <summary>
    /// Installer and resolver of the host-view preview hook.
    /// </summary>
    /// <remarks>
    /// Stateless by design: every request walks TypeCache and instantiates
    /// only the matched builder, so no registry can go stale across domain
    /// state changes. Placeholder policy lives here: missing config is an
    /// info placeholder; a missing, ambiguous, or throwing builder is an
    /// error placeholder, always paired with a console log.
    /// </remarks>
    [InitializeOnLoad]
    internal static class CollectionHostViewPreviewBridge
    {
        static CollectionHostViewPreviewBridge()
        {
            CollectionHostView.EditorPreviewFactory = BuildPreview;
        }

        private static VisualElement BuildPreview(CollectionViewConfig config)
        {
            if (config == null)
                return PreviewPlaceholderFactory.MissingConfig();

            var configType = config.GetType();
            var builderType = ResolveBuilderType(configType, out var ambiguous);
            if (ambiguous != null)
            {
                Debug.LogError(
                    $"Ambiguous preview builders for {configType.Name}: {string.Join(", ", ambiguous)}",
                    config);
                return PreviewPlaceholderFactory.Error(
                    $"Ambiguous preview builders for {configType.Name} (see console)");
            }

            if (builderType == null)
                return PreviewPlaceholderFactory.Error($"No preview builder for {configType.Name}");

            try
            {
                var builder = (CollectionViewPreviewBuilder)Activator.CreateInstance(builderType);
                return builder.Build(config);
            }
            catch (Exception e)
            {
                Debug.LogException(e, config);
                return PreviewPlaceholderFactory.Error(
                    $"{builderType.Name} threw {e.GetType().Name} (see console)");
            }
        }

        // Most-specific match wins: the exact config type first, then its base
        // types. Two builders declaring the same config type is an ambiguity,
        // never an arbitrary pick — TypeCache order is not deterministic — and
        // it blocks the base-type fallback rather than silently skipping past
        // the broken level.
        private static Type ResolveBuilderType(Type configType, out List<string> ambiguous)
        {
            ambiguous = null;
            for (var t = configType; t != null && typeof(CollectionViewConfig).IsAssignableFrom(t); t = t.BaseType)
            {
                Type match = null;
                foreach (var builderType in TypeCache.GetTypesDerivedFrom<CollectionViewPreviewBuilder>())
                {
                    if (builderType.IsAbstract || ConfigTypeOf(builderType) != t)
                        continue;

                    if (match == null)
                    {
                        match = builderType;
                    }
                    else
                    {
                        ambiguous ??= new List<string> { match.Name };
                        ambiguous.Add(builderType.Name);
                    }
                }

                if (ambiguous != null)
                    return null;
                if (match != null)
                    return match;
            }

            return null;
        }

        // The generic argument of CollectionViewPreviewBuilder<T> is the
        // builder's registration key; builders deriving the non-generic base
        // directly are not discoverable.
        private static Type ConfigTypeOf(Type builderType)
        {
            for (var t = builderType; t != null; t = t.BaseType)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(CollectionViewPreviewBuilder<>))
                    return t.GetGenericArguments()[0];
            }

            return null;
        }
    }
}
