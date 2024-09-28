# Changelog

#### v0.4.20 (Nebula 0.9.11)
\- Suppress NRE in SphereOpt.InstDysonShellRenderer.RenderShells  

#### v0.4.19 (Nebula 0.9.11)
\- Update UXAssist v1.2.4 compat  
\- Suppress tutorial tips  

#### v0.4.18 (Nebula 0.9.10)
\- Fix IndexOutOfRangeException in SpaceSector.RemoveEnemyWithComponents (IL_026A)  
\- Update stacktracer format  
\- Revert AssemblerVerticalConstruction compat (hidden)  
\- Stop CommonAPI "Loading registered asset..." debug log which triggered by TheyComeFromVoid  
\- Fix NRE in SphereOpt.InstDysonShellRenderer.RenderShells (IL_0117)  

#### v0.4.17 (Nebula 0.9.10)
\- Fix half growth dark fog base keep regenerating  
\- Fix combat drones doesn't increase ground base threat  
\- Remove AssemblerVerticalConstruction compat  

#### v0.4.16 (Nebula 0.9.9)
\- Stop DF relays landing on planet with 7 or more powered planetary shield generators  
\- Suppress IndexOutOfRangeException in NearColliderLogic.UpdateCursorNear  
\- Update UXAssist v1.1.6 compat  

#### v0.4.15 (Nebula 0.9.8)
\- Suppress IndexOutOfRangeException in BuildTool_Path.DeterminePreviews  
\- Suppress IndexOutOfRangeException in CargoTraffic.SetBeltState  
\- Suppress NREin BGMController.UpdateLogic  

#### v0.4.14 (Nebula 0.9.7)
\- Update FactoryLocator support version to 1.3.0.  
\- Fix multiplayer chat settings - `show battle message` doesn't work  
\- Fix NRE in NebulaPatcher.Patches.Transpilers.UIStatisticsWindow_Transpiler+<>c.<ComputePowerTab_Transpiler>b__2_9 (System.Int64 factoryIndex) [0x00025]  
\- Suppress IndexOutOfRangeException in EnemyDFGroundSystem.CalcFormsSupply  

#### v0.4.13 (Nebula 0.9.6)
\- Fix IndexOutOfRangeException in EnemyUnitComponent.RunBehavior_Defense_Ground(IL_028B)  
\- Fix NRE in Bomb_Explosive.TickSkillLogic(IL_03BE)  

#### v0.4.12 (Nebula 0.9.5)
\- Update MoreMegaStructure v1.5.3 compat  
\- Remove GalacticScale v2.14.1 compat  

#### v0.4.11 (Nebula 0.9.5)
\- Temporarily disable ACH_BroadcastStar.OnGameTick  
\- Temporarily disable DF Relay landing message in client  
\- Add GalacticScale v2.14.1 compat  

#### v0.4.10 (Nebula 0.9.5)
\- Fix Last Save Time to use real world time  
\- Add SphereOpt v0.9.1 compat  
\- Add UXAssist v1.0.26 compat  

<details>
<summary>Previous Changelog</summary>

#### v0.4.9 (Nebula 0.9.4)
\- Fix `NgrokManager.IsNgrokActive` crash  
\- Add syncing for relics and skillpoints in TheyComeFromVoid 3.1.2 (WIP)  

#### v0.4.8 (Nebula 0.9.3)
\- Add TheyComeFromVoid 3.1.0 compat (WIP)  
\- Fix divide by zero error in AssemblerVerticalConstruction  

#### v0.4.7 (Nebula 0.9.2)
\- Add DSPAutoSorter v1.2.11 compat  
\- Add AssemblerVerticalConstruction v1.1.4 compat (WIP)  
\- Fix hp bar remain after the game first load  

#### v0.4.6 (Nebula 0.9.2)
\- Fix ILS errors in client.  

#### v0.4.5 (Nebula 0.9.2)
\- Fix inventory size error in client.  
\- Fix EnemyFormation.RemoveUnit error in client.  

#### v0.4.4 (Nebula 0.9.2)
\- Attempt to fix some issues in client.  

#### v0.4.3 (Nebula 0.9.1)
\- Fix an issue that player data is clean up wrongly in server.  

#### v0.4.2 (Nebula 0.9.1)
\- Add new dependency DSPModSave.  
\- Make construction drones only launch if the current player is the cloest one or within 15m.  
\- Clear old playerSaves when server start.  
\- Suppress Enemy TickLogic excpetion to show only once per session.

#### v0.4.1 (Nebula 0.9.0)
\- MoreMegaStructure v1.3.8: Sync star cannon and fix errors.  
\- PlanetFinder v1.1.3: Fix error in multiplayer lobby.  
\- Remove DSPMarker support.  

#### v0.4.0 (Nebula 0.9.0 pre-release)
\- Hotfix: Tempoarily disable drone syncing.  
\- Remove Bottleneck, DSPTransportStat, Dustbin, TheyComeFromVoid support.  
\- Temporily disable BlueprintTweaks, DSPMarker, MoreMegaStructure, PlanetFinder support.  

</details>


## Nebula version 0.8.14

<details>
<summary>Previous Changelog</summary>

#### v0.3.1 (Nebula 0.8.14)
\- Hotfix: Fix NRE error in `StationUIManager.UpdateStorage.`  
\- Hotfix: Load dyson sphere when click on star view on the starmap.  
\- TheyComeFromVoid v2.2.8: Add remote cannons to let planets not loaded yet to fire weapons. Bug fixes.  

#### v0.3.0 (Nebula 0.8.14)
\- TheyComeFromVoid v2.2.8: Add Droplet syncing and fixes  
\- Remove BigFormingSize from incompat list  

#### v0.2.3 (Nebula 0.8.13)
\- Hotfix: Add compat to mods that increase reform brush size.  
\- Hotfix: (Test) Reset planet physics & audio when arriving at a planet.  
\- MoreMegaStructure v1.1.11: Fix receivers requested power flicks on clients.  

#### v0.2.2 (Nebula 0.8.13)
\- Hotfix: Fix an error when saving game.  
\- Bottleneck v1.0.15: Fixed an error that occurred on the host when the client was using different proliferator settings.  

#### v0.2.1 (Nebula 0.8.13)
\- Fix a bug that client can't change station storage.  
\- DSP Belt Reverse Direction is no longer supported due to vanilla has the function now.  

#### v0.2.0 (Nebula 0.8.13)
\- Hotfix: Fix mecha animation when 3rd player join.  
\- Update FactoryLocator support version to 1.2.0  
\- TheyComeFromVoid: Add StarFortress syncing  

#### v0.1.12 (Nebula 0.8.12)
\- Fix BlueprintTweak 1.5.9  
\- Support Dustbin 1.2.1  
\- Update FactoryLocator support version to 1.1.0  
\- Update Auxilaryfunction support version to 2.0.1  
\- MoreMegaStructure: Fix RefreshProduceSpeedText error  
\- TheyComeFromVoid: Add EnemyShips event syncing  

#### v0.1.11 (Nebula 0.8.12)
\- Hotfix: Sync Flip Whole Path button for belts in DSP 0.9.27.15466  
\- Support SplitterOverBelt 1.1.3  
\- Support TheyComeFromVoid 2.1.2  
\- Update Auxilaryfunction support version to 1.8.9  

#### v0.1.10 (Nebula 0.8.12)
\- Fix sandbox tool enable syncing.  
\- Show multiplayer name in starmap for own player.  

#### v0.1.9 (Nebula 0.8.12)
\- Support FactoryLocator 1.0.1  
\- Update MoreMegaStructure support version to 1.1.4  
\- Hide server ip and port during login & reconnect.  
\- Show the diff count of local & remote mod list when client login.  

#### v0.1.8 (Nebula 0.8.12)
\- Hotfix: Fix trash warning (error when there are litters on host and client join, positions doen't sync)  
\- Hotfix: Fix InserterOffsetCorrection which may cause desync that sorters don't work on one end.  
\- Hotfix: Fix SplitterPriorityChange packet.  

#### v0.1.7 (Nebula 0.8.12)  
\- Hotfix: Fix infinite tech level desync in client.  
\- Hotfix: Fix that rock destroy on remote planet show effects on local planet.  
\- Show possible mod patches from stacktraces on error report.  

#### v0.1.6 (Nebula 0.8.12)  
\- Hotfix: Fix error on host when client put a storage chest on a logisitics distributor on remote planets.  
\- Update PlanetFinder support version to 1.0.0.  

#### v0.1.5 (Nebula 0.8.11)  
\- Fix mod data doesn't sync correctly for another clients.  
\- Fix client mecha spawning position.  

#### v0.1.4 (Nebulad 0.8.11)  
\- Hotfix: Fix host sometimes get error when client request logistic on other planets.  
\- Hotfix for GS2 star detail doesn't display correctly for clients.  

#### v0.1.3 (Nebulad 0.8.11)
\- Hotfix: Fix logistic bots errors.  
\- Fix client error when host reverse belts on a remote planet.  

#### v0.1.2 (Nebula 0.8.10)
\- Support DSPOptimizations  

#### v0.1.1 (Nebula 0.8.10)
\- Support AutoStationConfig, Auxilaryfunction.  
\- Fix advance miner power usage abnormal of AutoStationConfig.   

#### v0.1.0 (Nebula 0.8.8)
\- Support DSPTransportStat, PlanetFinder, DSPFreeMechaCustom, MoreMegaStructure.  
\- Fix DSPMarker didn't refresh marker when local planet changed.  

#### v0.0.1  
\- Initial release. (Game Version 0.9.25.12201)

</details>
