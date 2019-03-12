﻿using ICities;
using UnityEngine;

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;

using FineRoadAnarchy.Redirection;
using FineRoadAnarchy.Detours;

namespace FineRoadAnarchy
{
    public class FineRoadAnarchy : MonoBehaviour
    {
        public const string settingsFileName = "FineRoadAnarchy";

        public static FineRoadAnarchy instance;

        public static FastList<NetInfo> bendingPrefabs = new FastList<NetInfo>();

        public static UIButton chirperButton;
        public static UITextureAtlas chirperAtlasAnarchy;
        public static UITextureAtlas chirperAtlasNormal;
        
        public NetTool m_netTool;

        private OptionsPanel m_panel;

        private int m_tries;

        public void Start()
        {
            try
            {
                m_netTool = GameObject.FindObjectOfType<NetTool>();
                if (m_netTool == null)
                {
                    DebugUtils.Log("Net Tool not found");
                    enabled = false;
                    return;
                }

                bendingPrefabs.Clear();

                int count = PrefabCollection<NetInfo>.PrefabCount();
                for (uint i = 0; i < count; i++)
                {
                    NetInfo prefab = PrefabCollection<NetInfo>.GetPrefab(i);
                    if (prefab != null)
                    {
                        if (prefab.m_enableBendingSegments)
                        {
                            bendingPrefabs.Add(prefab);
                        }
                    }
                }

                //Redirector<NetInfoDetour>.Deploy();
                collision = (ToolManager.instance.m_properties.m_mode & ItemClass.Availability.AssetEditor) == ItemClass.Availability.None;

                if (chirperAtlasAnarchy == null)
                {
                    LoadChirperAtlas();
                }

                chirperButton = UIView.GetAView().FindUIComponent<UIButton>("Zone");

                if (m_panel == null)
                {
                    m_tries = 0;
                    m_panel = UIView.GetAView().AddUIComponent(typeof(OptionsPanel)) as OptionsPanel;
                }
                else
                {
                    m_panel.m_anarchy.isChecked = false;
                    m_panel.m_bending.isChecked = true;
                    m_panel.m_snapping.isChecked = true;
                    m_panel.m_collision.isChecked = collision;
                }

                DebugUtils.Log("Initialized");
            }
            catch(Exception e)
            {
                DebugUtils.Log("Start failed");
                DebugUtils.LogException(e);
                enabled = false;
            }
        }
        
        public void Update()
        {
            try
            {
                if (m_tries < 5)
                {
                    UIPanel frtPanel = UIView.GetAView().FindUIComponent<UIPanel>("FRT_ToolOptionsPanel");

                    if (frtPanel != null)
                    {
                        DebugUtils.Log("Fine Road Tool window found");

                        frtPanel.height += m_panel.height + 8;

                        frtPanel.AttachUIComponent(m_panel.gameObject);
                        m_panel.relativePosition = new Vector3(8, frtPanel.height - m_panel.height - 8);
                        m_panel.width = frtPanel.width - 16;

                        frtPanel.GetComponentInChildren<UIDragHandle>().height = frtPanel.height;

                        m_tries = 5;
                    }

                    m_tries++;
                }
                else if (m_tries == 5)
                {
                    DebugUtils.Log("Fine Road Tool window not found");

                    UIMainWindow window = UIView.GetAView().AddUIComponent(typeof(UIMainWindow)) as UIMainWindow;

                    window.AttachUIComponent(m_panel.gameObject);
                    window.size = new Vector2(228, 180);
                    m_panel.relativePosition = new Vector3(8, 28);
                    m_panel.width = window.width - 16;

                    window.height = 36 + m_panel.height;

                    m_tries++;
                }
            }
            catch (Exception e)
            {
                m_tries = 6;

                DebugUtils.Log("Update failed");
                DebugUtils.LogException(e);
            }
        }

        public void OnDestroy()
        {
            //Redirector<NetInfoDetour>.Revert();
            anarchy = false;
        }

        public static bool anarchy
        {
            get
            {
                return Redirector<NetToolDetour>.IsDeployed();
            }

            set
            {
                if(anarchy != value)
                {
                    if(value)
                    {
                        DebugUtils.Log("Enabling anarchy");
                        Redirector<NetToolDetour>.Deploy();
                        Redirector<BuildingToolDetour>.Deploy();
                        Redirector<RoadAIDetour>.Deploy();
                        Redirector<PedestrianPathAIDetour>.Deploy();
                        Redirector<TrainTrackAIDetour>.Deploy();
                        Redirector<NetAIDetour>.Deploy();

                        if (chirperButton != null && chirperAtlasAnarchy != null)
                        {
                            chirperAtlasNormal = chirperButton.atlas;
                            chirperButton.atlas = chirperAtlasAnarchy;
                        }
                    }
                    else
                    {
                        DebugUtils.Log("Disabling anarchy");
                        Redirector<NetToolDetour>.Revert();
                        Redirector<BuildingToolDetour>.Revert();
                        Redirector<RoadAIDetour>.Revert();
                        Redirector<PedestrianPathAIDetour>.Revert();
                        Redirector<TrainTrackAIDetour>.Revert();
                        Redirector<NetAIDetour>.Revert();

                        if (chirperButton != null && chirperAtlasNormal != null)
                        {
                            chirperButton.atlas = chirperAtlasNormal;
                        }
                    }
                }
            }
        }

        public static bool bending
        {
            get
            {
                return bendingPrefabs.m_size > 0 && bendingPrefabs.m_buffer[0].m_enableBendingSegments;
            }

            set
            {
                if (bending != value)
                {
                    for (int i = 0; i < bendingPrefabs.m_size; i++)
                    {
                        bendingPrefabs.m_buffer[i].m_enableBendingSegments = value;
                    }
                }
            }
        }

        public static bool snapping
        {
            get
            {
                return !Redirector<NetInfoDetour>.IsDeployed();
            }

            set
            {
                if (value != snapping)
                {
                    if (value)
                    {
                        DebugUtils.Log("Enabling snapping");
                        Redirector<NetInfoDetour>.Revert();
                    }
                    else
                    {
                        DebugUtils.Log("Disabling snapping");
                        Redirector<NetInfoDetour>.Deploy();
                    }
                }
            }
        }

        public static bool collision
        {
            get
            {
                return !Redirector<CollisionNetNodeDetour>.IsDeployed();
            }

            set
            {
                if (value != collision)
                {
                    if (value)
                    {
                        DebugUtils.Log("Enabling collision");
                        Redirector<CollisionBuildingManagerDetour>.Revert();
                        Redirector<CollisionNetManagerDetour>.Revert();
                        Redirector<CollisionNetNodeDetour>.Revert();
                        CollisionZoneBlockDetour.Revert();
                    }
                    else
                    {
                        DebugUtils.Log("Disabling collision");
                        Redirector<CollisionBuildingManagerDetour>.Deploy();
                        Redirector<CollisionNetManagerDetour>.Deploy();
                        Redirector<CollisionNetNodeDetour>.Deploy();
                        CollisionZoneBlockDetour.Deploy();
                    }
                }
            }
        }

        public static bool grid
        {
            get
            {
                return (ToolManager.instance.m_properties.m_mode & ItemClass.Availability.AssetEditor) != ItemClass.Availability.None;
            }

            set
            {
                if(value)
                {
                    ToolManager.instance.m_properties.m_mode = ToolManager.instance.m_properties.m_mode | ItemClass.Availability.AssetEditor;
                }
                else
                {
                    ToolManager.instance.m_properties.m_mode = ToolManager.instance.m_properties.m_mode & ~ItemClass.Availability.AssetEditor;
                }
            }
        }

        public void OnGUI()
        {
            try
            {
                if (!UIView.HasModalInput() && !UIView.HasInputFocus())
                {
                    Event e = Event.current;

                    // Checking key presses
                    if (OptionsKeymapping.toggleAnarchy.IsPressed(e))
                    {
                        m_panel.m_anarchy.isChecked = !m_panel.m_anarchy.isChecked;
                    }
                    else if (OptionsKeymapping.toggleBending.IsPressed(e))
                    {
                        m_panel.m_bending.isChecked = !m_panel.m_bending.isChecked;
                    }
                    else if (OptionsKeymapping.toggleSnapping.IsPressed(e))
                    {
                        m_panel.m_snapping.isChecked = !m_panel.m_snapping.isChecked;
                    }
                    else if (OptionsKeymapping.toggleCollision.IsPressed(e))
                    {
                        m_panel.m_collision.isChecked = !m_panel.m_collision.isChecked;
                    }
                    else if (m_panel.m_grid != null && OptionsKeymapping.toggleGrid.IsPressed(e))
                    {
                        m_panel.m_grid.isChecked = !m_panel.m_grid.isChecked;
                    }
                }
            }
            catch (Exception e)
            {
                DebugUtils.Log("OnGUI failed");
                DebugUtils.LogException(e);
            }
        }


        private void LoadChirperAtlas()
        {
            string[] spriteNames = new string[]
			{
				"Chirper",
                "ChirperChristmas",
                "ChirperChristmasDisabled",
                "ChirperChristmasFocused",
                "ChirperChristmasHovered",
                "ChirperChristmasPressed",
                "ChirperConcerts",
                "ChirperConcertsDisabled",
                "ChirperConcertsFocused",
                "ChirperConcertsHovered",
                "ChirperConcertsPressed",
                "Chirpercrown",
                "ChirpercrownDisabled",
                "ChirpercrownFocused",
                "ChirpercrownHovered",
                "ChirpercrownPressed",
                "ChirperDeluxe",
                "ChirperDeluxeDisabled",
                "ChirperDeluxeFocused",
                "ChirperDeluxeHovered",
                "ChirperDeluxePressed",
                "ChirperDisabled",
                "ChirperDisastersHazmat",
                "ChirperDisastersHazmatDisabled",
                "ChirperDisastersHazmatFocused",
                "ChirperDisastersHazmatHovered",
                "ChirperDisastersHazmatPressed",
                "ChirperDisastersPilot",
                "ChirperDisastersPilotDisabled",
                "ChirperDisastersPilotFocused",
                "ChirperDisastersPilotHovered",
                "ChirperDisastersPilotPressed",
                "ChirperDisastersWorker",
                "ChirperDisastersWorkerDisabled",
                "ChirperDisastersWorkerFocused",
                "ChirperDisastersWorkerHovered",
                "ChirperDisastersWorkerPressed",
                "ChirperFocused",
                "ChirperFootball",
                "ChirperFootballDisabled",
                "ChirperFootballFocused",
                "ChirperFootballHovered",
                "ChirperFootballPressed",
                "ChirperHovered",
                "ChirperIcon",
                "ChirperJesterhat",
                "ChirperJesterhatDisabled",
                "ChirperJesterhatFocused",
                "ChirperJesterhatHovered",
                "ChirperJesterhatPressed",
                "ChirperLumberjack",
                "ChirperLumberjackDisabled",
                "ChirperLumberjackFocused",
                "ChirperLumberjackHovered",
                "ChirperLumberjackPressed",
                "ChirperParkRanger",
                "ChirperParkRangerDisabled",
                "ChirperParkRangerFocused",
                "ChirperParkRangerHovered",
                "ChirperParkRangerPressed",
                "ChirperPressed",
                "ChirperRally",
                "ChirperRallyDisabled",
                "ChirperRallyFocused",
                "ChirperRallyHovered",
                "ChirperRallyPressed",
                "ChirperRudolph",
                "ChirperRudolphDisabled",
                "ChirperRudolphFocused",
                "ChirperRudolphHovered",
                "ChirperRudolphPressed",
                "ChirperSouvenirGlasses",
                "ChirperSouvenirGlassesDisabled",
                "ChirperSouvenirGlassesFocused",
                "ChirperSouvenirGlassesHovered",
                "ChirperSouvenirGlassesPressed",
                "ChirperSurvivingMars",
                "ChirperSurvivingMarsDisabled",
                "ChirperSurvivingMarsFocused",
                "ChirperSurvivingMarsHovered",
                "ChirperSurvivingMarsPressed",
                "ChirperTrafficCone",
                "ChirperTrafficConeDisabled",
                "ChirperTrafficConeFocused",
                "ChirperTrafficConeHovered",
                "ChirperTrafficConePressed",
                "ChirperTrainConductor",
                "ChirperTrainConductorDisabled",
                "ChirperTrainConductorFocused",
                "ChirperTrainConductorHovered",
                "ChirperTrainConductorPressed",
                "ChirperWintercap",
                "ChirperWintercapDisabled",
                "ChirperWintercapFocused",
                "ChirperWintercapHovered",
                "ChirperWintercapPressed",
                "ChirperZookeeper",
                "ChirperZookeeperDisabled",
                "ChirperZookeeperFocused",
                "ChirperZookeeperHovered",
                "ChirperZookeeperPressed",
                "EmptySprite",
                "ThumbChirperBeanie",
                "ThumbChirperBeanieDisabled",
                "ThumbChirperBeanieFocused",
                "ThumbChirperBeanieHovered",
                "ThumbChirperBeaniePressed",
                "ThumbChirperFlower",
                "ThumbChirperFlowerDisabled",
                "ThumbChirperFlowerFocused",
                "ThumbChirperFlowerHovered",
                "ThumbChirperFlowerPressed",
                "ThumbChirperTech",
                "ThumbChirperTechDisabled",
                "ThumbChirperTechFocused",
                "ThumbChirperTechHovered",
                "ThumbChirperTechPressed"
			};

            chirperAtlasAnarchy = ResourceLoader.CreateTextureAtlas("ChirperAtlasAnarchy", spriteNames, "FineRoadAnarchy.ChirperAtlas.");
        }
    }

    public class FineRoadAnarchyLoader : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            if (FineRoadAnarchy.instance == null)
            {
                // Creating the instance
                FineRoadAnarchy.instance = new GameObject("FineRoadAnarchy").AddComponent<FineRoadAnarchy>();
            }
            else
            {
                FineRoadAnarchy.instance.Start();
            }

            if (mode == LoadMode.LoadAsset || mode == LoadMode.NewAsset)
            {
                GameAreaManager.instance.m_maxAreaCount = GameAreaManager.AREAGRID_RESOLUTION * GameAreaManager.AREAGRID_RESOLUTION;
                for (int i = 0; i < GameAreaManager.instance.m_maxAreaCount; i++)
                {
                    GameAreaManager.instance.m_areaGrid[i] = i + 1;
                }
                GameAreaManager.instance.m_areaCount = GameAreaManager.instance.m_maxAreaCount;
            }
        }
    }
}
