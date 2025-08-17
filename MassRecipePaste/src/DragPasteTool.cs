﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace MassRecipePaste
{
    public class DragPasteTool : BuildTool
    {
        public bool isEnable;
        public bool isSelecting;
        public bool isPasting;
        public int pastedCount;

        bool castTerrain;
        bool castGround;
        Vector3 castGroundPos = Vector3.zero;
        Vector3 castGroundPosSnapped = Vector3.zero;
        int castObjectId;
        Vector3 castObjectPos;
        bool cursorValid;
        Vector3 cursorTarget;
        Vector3 startGroundPosSnapped = Vector3.zero;
        Vector3 lastGroundPosSnapped = Vector3.zero;
        public BPGratBox selectGratBox = BPGratBox.zero;
        BPGratBox lastSelectGratBox = BPGratBox.zero;
        BPGratBox selectArcBox = BPGratBox.zero;

        HashSet<int> selectObjIds;
        BuildPreview[] bpPool;
        AnimData[] animPool;
        ComputeBuffer animBuffer;

        int bpCursor = 1;
        int bpPoolCapacity;
        int[] bpRecycle;
        int bpRecycleCursor;

        #region BuildTool methods, modify from BuildTool_BlueprintCopy

        public override void _OnInit()
        {
            //Plugin.Log.LogDebug("Init\n" + System.Environment.StackTrace);
            selectObjIds = new HashSet<int>();
            SetDisplayPreviewCapacity(256);
        }

        public override void _OnFree()
        {
            selectObjIds = null;
            FreeBuildPreviews();
            isEnable = false;
            isSelecting = false;
            isPasting = false;
            //Plugin.Log.LogDebug("Free");
        }

        public override void _OnOpen()
        {
            //Plugin.Log.LogDebug("Open\n" + System.Environment.StackTrace);
            ClearSelection();
            ResetBuildPreviews();
            castTerrain = false;
            castGround = false;
            castGroundPos = Vector3.zero;
            startGroundPosSnapped = lastGroundPosSnapped = castGroundPosSnapped = Vector3.zero;
            lastSelectGratBox = selectGratBox = selectArcBox = BPGratBox.zero;
            castObjectId = 0;
            castObjectPos = Vector3.zero;
            cursorValid = false;
            cursorTarget = Vector3.zero;
            isSelecting = false;
        }

        public override void _OnClose()
        {
            ClearSelection();
            ResetBuildPreviews();
            startGroundPosSnapped = lastGroundPosSnapped = castGroundPosSnapped = Vector3.zero;
            lastSelectGratBox = selectGratBox = selectArcBox = BPGratBox.zero;
            castObjectId = 0;
            castObjectPos = Vector3.zero;
            cursorValid = false;
            cursorTarget = Vector3.zero;
            isEnable = false;
            isSelecting = false;
            isPasting = false;
            //Plugin.Log.LogDebug("Close");
        }

        public override void _OnTick(long time)
        {
            UpdateRaycast();
            Operating();
            if (active)
            {
                UpdatePreviewModels(actionBuild.model);
            }
        }

        public override bool DetermineActive()
        {
            // Test in PlayerAction_Build.GameTick
            // If true, open the tool; otherwise close the tool
            return isEnable && actionBuild.blueprintMode == EBlueprintMode.None && controller.cmd.mode == 0;
        }

        public override void EscLogic()
        {
            bool outsideGUI = !VFInput.onGUI && VFInput.inScreen && !VFInput.inputing;
            bool escape = VFInput.escKey.onDown || VFInput.escape;
            bool rtsCancel = !VFInput._godModeMechaMove && VFInput.rtsCancel.onDown && outsideGUI;
            bool exit = rtsCancel || escape;

            if (exit)
            {
                player.SetHandItems(0, 0);
                _Close();
                actionBuild.Close();
            }

            if (escape) VFInput.UseEscape();
            if (rtsCancel) VFInput.UseMouseRight();
        }

        public override void UpdatePreviewModels(BuildModel model)
        {
            for (int i = 1; i < bpCursor; i++)
            {
                BuildPreview buildPreview = bpPool[i];
                if (buildPreview != null && buildPreview.bpgpuiModelId > 0 && buildPreview.isConnNode)
                {
                    if (buildPreview.objId > 0)
                    {
                        factory.cargoTraffic.SetBeltSelected(factory.entityPool[buildPreview.objId].beltId);
                    }
                    else
                    {
                        uint color = (uint)buildPreview.desc.beltSpeed;
                        if (buildPreview.outputObjId == 0 || buildPreview.inputObjId == 0 || buildPreview.coverbp != null)
                        {
                            model.connRenderer.AddBlueprintBeltMajorPoint(buildPreview.lpos, buildPreview.lrot, color);
                        }
                        else
                        {
                            model.connRenderer.AddBlueprintBeltPoint(buildPreview.lpos, buildPreview.lrot, color);
                        }
                    }
                    //model.connRenderer.AddXSign(buildPreview.lpos, buildPreview.lrot);
                }
            }
        }

        #endregion

        public void UpdateRaycast()
        {
            // Full copy from BuildTool_BlueprintCopy.UpdateRaycast
            castTerrain = false;
            castGround = false;
            castGroundPos = Vector3.zero;
            castGroundPosSnapped = Vector3.zero;
            castObjectId = 0;
            castObjectPos = Vector3.zero;
            cursorValid = false;
            cursorTarget = Vector3.zero;
            if (!VFInput.onGUI && VFInput.inScreen)
            {
                const int layerMask = 8720;
                castGround = Physics.Raycast(mouseRay, out RaycastHit raycastHit, 800f, layerMask, QueryTriggerInteraction.Collide);
                if (castGround)
                {
                    Layer layer = (Layer)raycastHit.collider.gameObject.layer;
                    castTerrain = layer == Layer.Terrain || layer == Layer.Water;
                    castGroundPos = controller.cmd.test = controller.cmd.target = raycastHit.point;
                    castGroundPosSnapped = actionBuild.planetAux.Snap(castGroundPos, castTerrain);
                    castGroundPosSnapped = castGroundPosSnapped.normalized * (planet.realRadius + 0.2f);
                    controller.cmd.test = castGroundPosSnapped;
                    Vector3 normalized = castGroundPosSnapped.normalized;
                    if (Physics.Raycast(new Ray(castGroundPosSnapped + normalized * 10f, -normalized), out raycastHit, 20f, layerMask,
                        QueryTriggerInteraction.Collide))
                    {
                        controller.cmd.test = raycastHit.point;
                    }

                    cursorTarget = castGroundPosSnapped;
                    cursorValid = true;
                }

                int castAllCount = controller.cmd.raycast.castAllCount;
                RaycastData[] castAll = controller.cmd.raycast.castAll;
                int num = 0;
                for (int i = 0; i < castAllCount; i++)
                {
                    if (castAll[i].objType == EObjectType.Entity || castAll[i].objType == EObjectType.Prebuild)
                    {
                        num = castAll[i].objType == EObjectType.Entity ? castAll[i].objId : -castAll[i].objId;
                        break;
                    }
                }

                if (num != 0)
                {
                    castObjectId = num;
                    castObjectPos = GetObjectPose(num).position;
                    cursorTarget = castObjectPos;
                    controller.cmd.test = castObjectPos;
                    castGroundPosSnapped = castGroundPos = castObjectPos;
                    castGroundPosSnapped = castGroundPosSnapped.normalized * (planet.realRadius + 0.2f);
                    controller.cmd.test = castGroundPosSnapped;
                    Vector3 normalized2 = castGroundPosSnapped.normalized;
                    if (Physics.Raycast(new Ray(castGroundPosSnapped + normalized2 * 10f, -normalized2), out raycastHit, 20f, layerMask,
                        QueryTriggerInteraction.Collide))
                    {
                        controller.cmd.test = raycastHit.point;
                    }

                    cursorTarget = castGroundPosSnapped;
                    cursorValid = true;
                }
            }

            controller.cmd.state = cursorValid ? 1 : 0;
            controller.cmd.target = cursorValid ? cursorTarget : Vector3.zero;
        }

        public void Operating()
        {
            if (!isSelecting && VFInput.blueprintCopyOperate0.onDown && cursorValid)
            {
                isSelecting = true;
                startGroundPosSnapped = castGroundPosSnapped;
                lastGroundPosSnapped = startGroundPosSnapped;
                InitSelectGratBox();
                VFInput.UseMouseLeft();
            }

            bool point = (castGroundPosSnapped - startGroundPosSnapped).sqrMagnitude > 0.01f;

            bool onDown = VFInput.blueprintCopyOperate0.onDown || VFInput.blueprintCopyOperate1.onDown;
            if (isSelecting && (onDown && cursorValid))
            {
                // Paste recipe to selecting buildings
                PasteAction();

                ResetBuildPreviews();
                isSelecting = false;
                VFInput.UseMouseLeft();
                VFInput.UseEnterConfirm();

                // Exit after pasting
                _Close();
            }
            else if (isSelecting && VFInput.rtsCancel.onUp)
            {
                isSelecting = false;
                startGroundPosSnapped = castGroundPosSnapped;
                lastGroundPosSnapped = startGroundPosSnapped;
                ClearSelection();
                ResetBuildPreviews();
            }

            if (isSelecting)
            {
                // Change cursor to indicate the selecting mode
                UICursor.SetCursor(ECursor.TargetIn);
                DetermineSelectGratBox();
                if (lastSelectGratBox != selectGratBox)
                {
                    DetermineAddSelection();
                    lastSelectGratBox = selectGratBox;
                }                
            }
            else
            {
                startGroundPosSnapped = castGroundPosSnapped;
                ReDeterminePreviews();
            }

            if (point)
            {
                ReDeterminePreviews();
            }

            // Display selecting building count on cursor
            ref var clipboard = ref BuildingParameters.clipboard;
            if ((clipboard.type == BuildingType.Assembler || clipboard.type == BuildingType.Lab) && clipboard.recipeId != 0)
            {
                actionBuild.model.cursorText = "MassRecipePaste\r\n" 
                    + clipboard.recipeType + " (" + LDB.RecipeName(clipboard.recipeId) + ") : " + selectObjIds.Count;
            }
            else
            {
                actionBuild.model.cursorText = "MassRecipePaste\r\n" 
                    + clipboard.type + " : " + selectObjIds.Count;
            }
        }

        public void PasteAction()
        {
            Plugin.Log.LogInfo("PasteAction " + BuildingParameters.clipboard.type + " - " + BuildingParameters.clipboard.recipeType);
            isPasting = true;
            pastedCount = 0;
            foreach (var objectId in selectObjIds)
            {
                factory.PasteBuildingSetting(objectId);
            }
            isPasting = false;
            isSelecting = false;
            ClearSelection();
            if (pastedCount > 0)
            {
                string text = BuildingParameters.clipboard.PastedTipText() + " (" + pastedCount + ")";
                UIRealtimeTip.Popup(text, false, 0);
                Plugin.Log.LogDebug(text);
            }
        }

        private void InitSelectGratBox()
        {
            BlueprintUtils.GetMinimumGratBox(startGroundPosSnapped.normalized, ref selectGratBox);
            selectArcBox = selectGratBox;
            if (selectArcBox.y >= 1.5707864f)
            {
                selectArcBox.y = selectArcBox.w = 1.5707964f;
                selectArcBox.z = selectArcBox.x + 628.31854f;
            }
            else if (selectArcBox.y <= -1.5707864f)
            {
                selectArcBox.y = selectArcBox.w = -1.5707964f;
                selectArcBox.z = selectArcBox.x + 628.31854f;
            }

            lastSelectGratBox = selectGratBox;
        }

        public void DetermineSelectGratBox()
        {
            if (cursorValid)
            {
                float longitudeRad = BlueprintUtils.GetLongitudeRad(castGroundPosSnapped.normalized);
                float longitudeRad2 = BlueprintUtils.GetLongitudeRad(lastGroundPosSnapped.normalized);
                float latitudeRad = BlueprintUtils.GetLatitudeRad(castGroundPosSnapped.normalized);
                bool flag = latitudeRad >= 1.5707864f || latitudeRad <= -1.5707864f;
                float num = flag ? 0f : longitudeRad - longitudeRad2;
                num = Mathf.Repeat(num + 3.1415927f, 6.2831855f) - 3.1415927f;
                selectArcBox.endLongitudeRad += num;
                selectArcBox.endLatitudeRad = latitudeRad;
                selectGratBox = selectArcBox;
                selectGratBox.x = selectArcBox.x < selectArcBox.z ? selectArcBox.x : selectArcBox.z;
                selectGratBox.z = selectArcBox.x > selectArcBox.z ? selectArcBox.x : selectArcBox.z;
                if (selectArcBox.x < selectArcBox.z)
                {
                    if (selectGratBox.z > selectGratBox.x + 6.2831855f - 1E-05f - 4E-06f)
                    {
                        selectGratBox.z = selectGratBox.x + 6.2831855f - 1E-05f - 4E-06f;
                    }

                    selectGratBox.z = Mathf.Repeat(selectGratBox.z + 3.1415927f, 6.2831855f) - 3.1415927f;
                }
                else
                {
                    if (selectGratBox.x < selectGratBox.z - 6.2831855f + 1E-05f + 4E-06f)
                    {
                        selectGratBox.x = selectGratBox.z - 6.2831855f + 1E-05f + 4E-06f;
                    }

                    selectGratBox.x = Mathf.Repeat(selectGratBox.x + 3.1415927f, 6.2831855f) - 3.1415927f;
                }

                selectGratBox.y = selectArcBox.y < selectArcBox.w ? selectArcBox.y : selectArcBox.w;
                selectGratBox.w = selectArcBox.y > selectArcBox.w ? selectArcBox.y : selectArcBox.w;
                float longitude = BlueprintUtils.GetLongitudeRadPerGrid(Mathf.Abs(castGroundPosSnapped.y) < Mathf.Abs(startGroundPosSnapped.y)
                    ? castGroundPosSnapped.normalized
                    : startGroundPosSnapped.normalized) * 0.33f;
                selectGratBox.Extend(longitude, 0.002f);
                if (!flag)
                {
                    lastGroundPosSnapped = castGroundPosSnapped;
                }
            }
        }

        public void DetermineAddSelection()
        {
            selectObjIds.Clear();

            if (Mathf.Abs(selectArcBox.x - selectArcBox.z) < 0.01f && Mathf.Abs(selectArcBox.y - selectArcBox.w) < 0.01f && castObjectId != 0)
            {
                if (ShouldAddObject(castObjectId))
                {
                    selectObjIds.Add(castObjectId);
                }
            }
            else
            {
                EntityData[] entityPool = factory.entityPool;
                int entityCursor = factory.entityCursor;
                for (int i = 1; i < entityCursor; i++)
                {
                    int item = i;
                    if (entityPool[i].id == i && selectGratBox.InGratBox(entityPool[i].pos))
                    {
                        if (ShouldAddObject(item))
                        {
                            selectObjIds.Add(item);
                        }
                    }
                }

                PrebuildData[] prebuildPool = factory.prebuildPool;
                int prebuildCursor = factory.prebuildCursor;
                for (int i = 1; i < prebuildCursor; i++)
                {
                    int item = -i;
                    if (prebuildPool[i].id == i && selectGratBox.InGratBox(prebuildPool[i].pos))
                    {
                        if (ShouldAddObject(item))
                        {
                            selectObjIds.Add(item);
                        }
                    }
                }
            }

            DeterminePreviews();
        }

        public bool ShouldAddObject(int objId)
        {            
            bool result = BuildingParameters.clipboard.CanPasteToFactoryObject(objId, factory);
            // CanPasteToFactoryObject大多數情況都是簡單判斷建築種類是否相同
            // 對於Assembler和Lab會額外判斷配方建築類型是否吻合和配方是否相同
            // 但是因為沒有判斷增產方式是否不同, 所以我們要在這裡額外補上

            if (result == false && objId > 0)
            {
                if (BuildingParameters.clipboard.type == BuildingType.Assembler || BuildingParameters.clipboard.type == BuildingType.Lab)
                {
                    int recipeId = BuildingParameters.clipboard.recipeId;
                    var entityPool = factory.entityPool;
                    var factorySystem = factory.factorySystem;
                    switch (BuildingParameters.clipboard.type)
                    {
                        case BuildingType.Assembler:
                            int assemblerId = entityPool[objId].assemblerId;
                            if (assemblerId == 0 || recipeId == 0 || factorySystem.assemblerPool[assemblerId].recipeId != recipeId) break;
                            if (BuildingParameters.clipboard.parameters != null && BuildingParameters.clipboard.parameters.Length >= 1)
                            {
                                bool forceAccMode = BuildingParameters.clipboard.parameters[0] > 0;
                                result |= factorySystem.assemblerPool[assemblerId].forceAccMode != forceAccMode;
                            }
                            break;

                        case BuildingType.Lab:
                            int labId = entityPool[objId].labId;
                            if (labId == 0 || recipeId == 0) break;
                            if (true)
                            {
                                bool forceAccMode = BuildingParameters.clipboard.mode1 == 1;
                                result |= factorySystem.labPool[labId].forceAccMode != forceAccMode;
                            }
                            break;
                    }
                }
            }
            return result;
        }

        public void ClearSelection()
        {
            selectObjIds.Clear();
            lastSelectGratBox = selectGratBox = selectArcBox = BPGratBox.zero;
        }

        #region Previews

        public void ReDeterminePreviews()
        {
            ResetBuildPreviews();
            foreach (int objId in selectObjIds)
            {
                BuildPreview buildPreview = GetBuildPreview(objId);
                AddBPGPUIModel(buildPreview);
            }

            SyncAnimBuffer();
            planet.factoryModel.bpgpuiManager.animBuffer = animBuffer;
            planet.factoryModel.bpgpuiManager.SyncAllGPUBuffer();
        }

        public void DeterminePreviews()
        {
            var missing = new HashSet<int>(selectObjIds);
            for (int i = 1; i < bpCursor; i++)
            {
                BuildPreview buildPreview = bpPool[i];
                if (buildPreview != null && buildPreview.bpgpuiModelId > 0)
                {
                    if (!selectObjIds.Contains(buildPreview.objId))
                    {
                        if (buildPreview.bpgpuiModelInstIndex >= 0)
                            planet.factoryModel.bpgpuiManager.RemoveBuildPreviewModel(buildPreview.desc.modelIndex, buildPreview.bpgpuiModelInstIndex, false);
                        RemoveBuildPreview(i);
                    }
                    else
                    {
                        missing.Remove(buildPreview.objId);
                    }
                }
            }

            foreach (int objId in missing)
            {
                BuildPreview buildPreview = GetBuildPreview(objId);
                AddBPGPUIModel(buildPreview);
            }

            if (castObjectId != 0)
            {
                BuildPreview buildPreview3 = GetBuildPreview(castObjectId);
                AddBPGPUIModel(buildPreview3);
            }

            SyncAnimBuffer();
            planet.factoryModel.bpgpuiManager.animBuffer = animBuffer;
            planet.factoryModel.bpgpuiManager.SyncAllGPUBuffer();
        }

        public void AddBPGPUIModel(BuildPreview preview)
        {
            if (preview == null || preview.bpgpuiModelId <= 0)
            {
                return;
            }

            if (!preview.needModel)
            {
                return;
            }

            ModelProto modelProto = LDB.models.Select(preview.desc.modelIndex);
            Color32 color = new(44, 123, 250, 255); // cyan Configs.builtin.pasteConfirmOkColor

            if (modelProto.RendererType == 2)
            {
                GetInserterT1T2(preview.objId, out bool flag, out bool flag2);

                if (preview.objId > 0)
                {
                    animPool[preview.bpgpuiModelId] = factory.entityAnimPool[preview.objId];
                }

                animPool[preview.bpgpuiModelId].state = (uint)((color.r << 24) + (color.g << 16) + (color.b << 8) + color.a);
                planet.factoryModel.bpgpuiManager.AddBuildPreviewModel(preview.desc.modelIndex, out preview.bpgpuiModelInstIndex, preview.bpgpuiModelId,
                    preview.lpos, preview.lrot, preview.lpos2, preview.lrot2, flag ? 1 : 0, flag2 ? 1 : 0, false);
                return;
            }

            if (modelProto.RendererType == 3)
            {
                factory.ReadObjectConn(preview.objId, 14, out bool _, out int num, out int _);

                if (preview.objId > 0)
                {
                    animPool[preview.bpgpuiModelId] = factory.entityAnimPool[preview.objId];
                }

                animPool[preview.bpgpuiModelId].state = (uint)((color.r << 24) + (color.g << 16) + (color.b << 8) + color.a);
                planet.factoryModel.bpgpuiManager.AddBuildPreviewModel(preview.desc.modelIndex, out preview.bpgpuiModelInstIndex, preview.bpgpuiModelId,
                    preview.lpos, preview.lrot, num != 0 ? 1U : 0U, false);
                return;
            }

            if (preview.objId > 0)
            {
                animPool[preview.bpgpuiModelId] = factory.entityAnimPool[preview.objId];
            }

            animPool[preview.bpgpuiModelId].state = (uint)((color.r << 24) + (color.g << 16) + (color.b << 8) + color.a);
            if (preview.objId > 0 && preview.desc.isEjector)
            {
                animPool[preview.bpgpuiModelId].power = factory.factorySystem.ejectorPool[factory.entityPool[preview.objId].ejectorId].localDir.z;
            }

            planet.factoryModel.bpgpuiManager.AddBuildPreviewModel(preview.desc.modelIndex, out preview.bpgpuiModelInstIndex, preview.bpgpuiModelId,
                preview.lpos, preview.lrot, false);
        }

        public void GeneratePreviewByObjId(BuildPreview preview, int objId)
        {
            ItemProto itemProto = GetItemProto(objId);
            PrefabDesc prefabDesc = GetPrefabDesc(objId);
            if (prefabDesc == null || itemProto == null)
            {
                preview.ResetAll();
                return;
            }

            Pose objectPose = GetObjectPose(objId);
            Pose pose = prefabDesc.isInserter ? GetObjectPose2(objId) : objectPose;
            preview.item = itemProto;
            preview.desc = prefabDesc;
            preview.lpos = objectPose.position;
            preview.lrot = objectPose.rotation;
            preview.lpos2 = objectPose.position;
            preview.lrot2 = objectPose.rotation;
            preview.objId = objId;
            preview.genNearColliderArea2 = 0f;
            if (preview.desc.lodCount > 0 && preview.desc.lodMeshes != null && preview.desc.lodMeshes[0] != null)
            {
                preview.needModel = true;
            }
            else
            {
                preview.needModel = false;
            }

            preview.isConnNode = prefabDesc.isBelt;
            if (prefabDesc.isBelt)
            {
                for (int i = 0; i < 4; i++)
                {
                    factory.ReadObjectConn(objId, i, out bool flag, out int num, out int _);
                    if (num != 0)
                    {
                        if (flag)
                        {
                            preview.outputObjId = num;
                        }
                        else if (preview.inputObjId == 0)
                        {
                            preview.inputObjId = num;
                        }
                        else
                        {
                            preview.coverbp = preview;
                        }
                    }
                }
            }

            if (prefabDesc.isInserter)
            {
                preview.lpos2 = pose.position;
                preview.lrot2 = pose.rotation;
            }
        }

        public void ResetBuildPreviews()
        {
            if (planet != null && planet.factoryModel != null && planet.factoryModel.bpgpuiManager != null)
            {
                planet.factoryModel.bpgpuiManager.Reset();
            }

            for (int i = 0; i < bpPool.Length; i++)
            {
                if (bpPool[i] != null)
                {
                    bpPool[i].ResetAll();
                }
            }

            Array.Clear(animPool, 0, bpPoolCapacity);
            Array.Clear(bpRecycle, 0, bpPoolCapacity);
            bpCursor = 1;
            bpRecycleCursor = 0;
            animBuffer.SetData(animPool);
        }

        public void FreeBuildPreviews()
        {
            if (planet != null && planet.factoryModel != null && planet.factoryModel.bpgpuiManager != null)
            {
                planet.factoryModel.bpgpuiManager.Reset();
            }

            for (int i = 0; i < bpPool.Length; i++)
            {
                if (bpPool[i] != null)
                {
                    bpPool[i].Free();
                    bpPool[i] = null;
                }
            }

            animPool = null;
            bpPool = null;
            bpCursor = 1;
            bpPoolCapacity = 0;
            bpRecycle = null;
            bpRecycleCursor = 0;
            if (animBuffer != null)
            {
                animBuffer.Release();
                animBuffer = null;
            }
        }

        private void SetDisplayPreviewCapacity(int newCapacity)
        {
            BuildPreview[] array = bpPool;
            AnimData[] sourceArray = animPool;
            bpPool = new BuildPreview[newCapacity];
            animPool = new AnimData[newCapacity];
            bpRecycle = new int[newCapacity];
            if (array != null)
            {
                Array.Copy(array, bpPool, newCapacity > bpPoolCapacity ? bpPoolCapacity : newCapacity);
                Array.Copy(sourceArray, animPool, newCapacity > bpPoolCapacity ? bpPoolCapacity : newCapacity);
            }

            bpPoolCapacity = newCapacity;
            animBuffer?.Release();
            animBuffer = new ComputeBuffer(newCapacity, 20, ComputeBufferType.Default);
        }

        public void RemoveBuildPreview(int id)
        {
            if (bpPool[id] != null && bpPool[id].bpgpuiModelInstIndex >= 0)
            {
                animPool[id].time = 0f;
                animPool[id].prepare_length = 0f;
                animPool[id].working_length = 0f;
                animPool[id].state = 0U;
                animPool[id].power = 0f;
                bpPool[id].ResetAll();
                int[] array = bpRecycle;
                int num = bpRecycleCursor;
                bpRecycleCursor = num + 1;
                array[num] = id;
            }
        }

        public BuildPreview GetBuildPreview(int objId)
        {
            int num;
            if (bpRecycleCursor > 0)
            {
                int[] array = bpRecycle;
                num = bpRecycleCursor - 1;
                bpRecycleCursor = num;
                int num2 = array[num];
                BuildPreview buildPreview = bpPool[num2];
                if (buildPreview == null)
                {
                    buildPreview = new BuildPreview();
                    bpPool[num2] = buildPreview;
                }

                GeneratePreviewByObjId(buildPreview, objId);
                animPool[num2] = default;
                buildPreview.previewIndex = num2;
                buildPreview.bpgpuiModelId = num2;
                return buildPreview;
            }

            num = bpCursor;
            bpCursor = num + 1;
            int num3 = num;
            if (num3 == bpPoolCapacity)
            {
                SetDisplayPreviewCapacity(bpPoolCapacity * 2);
            }

            BuildPreview buildPreview2 = bpPool[num3];
            if (buildPreview2 == null)
            {
                buildPreview2 = new BuildPreview();
                bpPool[num3] = buildPreview2;
            }

            GeneratePreviewByObjId(buildPreview2, objId);
            animPool[num3] = default;
            buildPreview2.previewIndex = num3;
            buildPreview2.bpgpuiModelId = num3;
            return buildPreview2;
        }

        public void SyncAnimBuffer()
        {
            animBuffer?.SetData(animPool);
        }

        #endregion

    }
}