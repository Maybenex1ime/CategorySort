using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LogosSDK.Core.Events;
using UnityEditor;
using UnityEngine;

namespace EventBus.Editor
{
    /// <summary>
    /// Editor window for debugging the event bus system.
    /// </summary>
    public class EventBusDebuggerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 1.0; // 1 second
        private List<EventTypeInfo> _eventTypes = new();

        // List of assemblies to skip (known problematic ones)
        private static readonly HashSet<string> _assembliesToSkip = new HashSet<string>
        {
            "mscorlib",
            "System",
            "System.Core",
            "JetBrains",
            "Mono.Cecil",
            "Unity.Cecil",
            "ExCSS.Unity",
            "ICSharpCode",
            "Newtonsoft.Json",
            "nunit.framework",
        };

        // Add menu item to open the window
        [MenuItem("Tools/Events/EventBus Debugger")]
        public static void ShowWindow()
        {
            var window = GetWindow<EventBusDebuggerWindow>("EventBus Debugger");
            window.minSize = new Vector2(600, 300);
            window.RefreshEventTypes();
        }

        private void OnEnable()
        {
            RefreshEventTypes();
        }

        private void Update()
        {
            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime >= RefreshInterval)
            {
                RefreshEventTypes();
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawEventTable();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshEventTypes();
            }

            if (GUILayout.Button("Clear Counters", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                // Clear counters logic would go here if we had counters
            }

            GUILayout.FlexibleSpace();

            bool newAutoRefresh = GUILayout.Toggle(_autoRefresh, "Auto-Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));
            if (newAutoRefresh != _autoRefresh)
            {
                _autoRefresh = newAutoRefresh;
                if (_autoRefresh)
                {
                    _lastRefreshTime = EditorApplication.timeSinceStartup;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawEventTable()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            // Table header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Event Type", EditorStyles.boldLabel, GUILayout.Width(300));
            GUILayout.Label("Sync Listeners", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("Async Listeners", EditorStyles.boldLabel, GUILayout.Width(100));
            GUILayout.Label("Total", EditorStyles.boldLabel, GUILayout.Width(50));
            GUILayout.Label("Test", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            // Table content
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_eventTypes.Count == 0)
            {
                EditorGUILayout.HelpBox("No event types found with active listeners.", MessageType.Info);
            }
            else
            {
                foreach (var eventType in _eventTypes)
                {
                    DrawEventTypeRow(eventType);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawEventTypeRow(EventTypeInfo eventType)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(eventType.TypeName, GUILayout.Width(300));
            GUILayout.Label(eventType.SyncListeners.ToString(), GUILayout.Width(100));
            GUILayout.Label(eventType.AsyncListeners.ToString(), GUILayout.Width(100));
            GUILayout.Label(eventType.TotalListeners.ToString(), GUILayout.Width(50));

            GUI.enabled = eventType.CanCreateInstance;
            if (GUILayout.Button("Raise", GUILayout.Width(50)))
            {
                RaiseTestEvent(eventType);
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshEventTypes()
        {
            _eventTypes.Clear();
            
            // Find all types that have invokers
            var invokerStoreType = typeof(InvokerStore<>);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            // Only scan assemblies that are likely to contain our event types
            var relevantAssemblies = assemblies.Where(ShouldScanAssembly).ToArray();
            
            foreach (var assembly in relevantAssemblies)
            {
                try
                {
                    ScanAssemblyForEventTypes(assembly, invokerStoreType);
                }
                catch (Exception ex)
                {
                    // Just log the error without details to avoid cascading errors
                    Debug.LogWarning($"Skipping assembly {assembly.GetName().Name}: {ex.GetType().Name}");
                }
            }
            
            // Sort by type name
            _eventTypes = _eventTypes.OrderBy(t => t.TypeName).ToList();
            
            _lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        private bool ShouldScanAssembly(Assembly assembly)
        {
            if (assembly.IsDynamic)
                return false;
                
            var assemblyName = assembly.GetName().Name;
            
            // Skip known system and third-party assemblies that won't contain our event types
            if (_assembliesToSkip.Any(name => assemblyName.StartsWith(name, StringComparison.OrdinalIgnoreCase)))
                return false;
                
            // Skip Unity's internal assemblies
            if (assemblyName.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase) &&
                !assemblyName.Contains("Game") &&
                !assemblyName.Contains("User"))
                return false;
                
            // Skip UnityEditor and UnityEngine internal assemblies
            if ((assemblyName == "UnityEditor" || assemblyName == "UnityEngine") ||
                assemblyName.StartsWith("UnityEditor.", StringComparison.OrdinalIgnoreCase) ||
                assemblyName.StartsWith("UnityEngine.", StringComparison.OrdinalIgnoreCase))
                return false;
                
            return true;
        }

        private void ScanAssemblyForEventTypes(Assembly assembly, Type invokerStoreType)
        {
            Type[] types;
            
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // If we can't load all types, work with the ones we can
                types = ex.Types.Where(t => t != null).ToArray();
            }
            
            foreach (var type in types)
            {
                // Skip types that are obviously not valid
                if (type == null || !type.IsPublic || type.IsGenericTypeDefinition || 
                    type.IsInterface || type.IsAbstract)
                    continue;
                
                // Skip types that can't be used as generic arguments
                if (!IsValidGenericArgument(type))
                    continue;
                
                try
                {
                    // Check if this type is used as a generic argument for InvokerStore<T>
                    var invokerStoreGenericType = invokerStoreType.MakeGenericType(type);
                    var invokerField = invokerStoreGenericType.GetField("Invoker", BindingFlags.Public | BindingFlags.Static);
                    
                    if (invokerField != null)
                    {
                        var invoker = invokerField.GetValue(null);
                        if (invoker != null)
                        {
                            // Get listener counts using reflection
                            var syncCountProp = invoker.GetType().GetProperty("SyncListenerCount");
                            var asyncCountProp = invoker.GetType().GetProperty("AsyncListenerCount");
                            
                            if (syncCountProp != null && asyncCountProp != null)
                            {
                                int syncCount = (int)syncCountProp.GetValue(invoker);
                                int asyncCount = (int)asyncCountProp.GetValue(invoker);
                                
                                // Only add types that have listeners
                                if (syncCount > 0 || asyncCount > 0)
                                {
                                    _eventTypes.Add(new EventTypeInfo
                                    {
                                        Type = type,
                                        TypeName = type.FullName,
                                        SyncListeners = syncCount,
                                        AsyncListeners = asyncCount,
                                        TotalListeners = syncCount + asyncCount,
                                        CanCreateInstance = HasParameterlessConstructor(type)
                                    });
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip any type that causes problems
                    continue;
                }
            }
        }

        private bool IsValidGenericArgument(Type type)
        {
            // Quick checks for obviously invalid types
            if (type == null || type.IsByRef || type.IsPointer || type == typeof(void) || 
                type == typeof(TypedReference) || type.IsGenericParameter)
                return false;
                
            // Check for problematic type names
            string fullName = type.FullName ?? string.Empty;
            if (fullName.Contains("TypedReference") ||
                fullName.Contains("ArgIterator") ||
                fullName.Contains("RuntimeArgumentHandle") ||
                fullName.Contains("RuntimeFieldHandle") ||
                fullName.Contains("RuntimeMethodHandle") ||
                fullName.Contains("RuntimeTypeHandle") ||
                fullName.Contains("InterpolatedString") ||
                fullName.Contains("FileSystemEntry") ||
                type.Name == "__ComObject")
                return false;
                
            // Additional safety check - try to create a simple generic with this type
            try
            {
                var listType = typeof(List<>).MakeGenericType(type);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool HasParameterlessConstructor(Type type)
        {
            if (type.IsValueType) return true; // Value types always have a parameterless constructor
            
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        private void RaiseTestEvent(EventTypeInfo eventType)
        {
            try
            {
                // Create an instance of the event type
                var instance = Activator.CreateInstance(eventType.Type);
                
                // Get the Fire<T> method on GlobalBus
                var fireMethod = typeof(GlobalBus).GetMethod("Fire")?.MakeGenericMethod(eventType.Type);
                
                // Invoke the Fire method with the instance
                fireMethod?.Invoke(Bus.Global, new[] { instance });
                
                Debug.Log($"Test event raised: {eventType.TypeName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error raising test event for {eventType.TypeName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Class to store information about an event type.
        /// </summary>
        private class EventTypeInfo
        {
            public Type Type { get; set; }
            public string TypeName { get; set; }
            public int SyncListeners { get; set; }
            public int AsyncListeners { get; set; }
            public int TotalListeners { get; set; }
            public bool CanCreateInstance { get; set; }
        }
    }
} 