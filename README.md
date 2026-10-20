[English](#english) · [简体中文](#简体中文)

# M1A1 Abrams AMP

Fork of [Cyances/M1A1AbramsAMP](https://github.com/Cyances/M1A1AbramsAMP) · Gunner, HEAT, PC! 0.1.0-alpha+20261014

## English

Big thanks to ATLAS/thebeninator for providing the Abrams and Bradley 50mm mod in the first place! All the required code was from his mods and I just figured out how to put the pieces together so that I could recreate the AMP round. I also would like to thank Swiss/SovGrenadier for the GLATGM implementation. Be sure to checkout their mods because they are great!

A mod for Gunner, HEAT, PC! Requires [MelonLoader](https://github.com/LavaGang/MelonLoader/)

If you get an error launching through Steam you will need to run the game executable directly.

![Screenshot_1](https://raw.githubusercontent.com/Cyances/M1A1AbramsAMP/master/Images/AMP%20vs%20SPW-60B.png)
![Screenshot_3](https://raw.githubusercontent.com/Cyances/M1A1AbramsAMP/master/Images/AMP%20vs%20T-55A.png)
![Screenshot_4](https://raw.githubusercontent.com/Cyances/M1A1AbramsAMP/master/Images/AMP%20vs%20T-55A%20XRay.png)
![AMP vs Mi-24](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/2727aa85-097b-41c6-8a31-6a6c8dd7af12)
![AMP Enhanced Optics v2](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/c538fba0-c967-429f-b91a-e6d338ec7a4e)

## AMP 2.7.6 Update:
<p>
	<ul>
	<li>Adapted to game version 20261014</li>
	<li>LAHAT top-attack mode: middle mouse toggles top-attack / direct fire; both locked and unlocked launches fly the climb–level–dive profile</li>
	<li>CROWS fixed, MK19 CROWS added</li>
	<li>120mm M256 Gun (with new model!)</li>
	<li>M1IP automatically converted to M1A1</li>
	<li>Tank upgrades based on real life developments (but guesstimated values) and hypothetical thinking</li>
     	<li>Configurable Abrams armor variants: base armor, Heavy Armor (+30% protection), Heavy Common (+45% protection), Situational Awareness (+85%) and Heavy Ultra (custom)</li>
     	<li>Configurable rounds (up to four slots and 50 max total rounds)</li>
	<li>Default M1A1 loadout: 20x M829A4, 12x M830A2, 12x XM1147, 6x LAHAT</li>
	<li>Default M1E1 loadout: 20x M829, 15x M830, 15x M830A1 (press key 2/3 to select M830/A1 [key 4 is just an empty rack of M830A1])</li>
	<li>APFSDS: M829, M829A1, M829A2, M829A3, M829A4</li>
    	<li>HEAT: M830, M830A1, M830A2, M830A3, XM1147</li>
    	<li>HE: XM1147, M908</li>
    	<li>GLATGM: LAHAT</li>
    	<li>XM1147 AMP-T multifuze round (point-detonate + time-delay or proximity)</li>
	<li>Anti-ERA/tandem warhead properties for AP and HEAT (modelled for (Super) Kontakt-1, Kontakt-5, ARAT and BRAT), requires at least NATO ERA v1.1.1b and/or Pact Increased Lethality v1.2.5+</li>
	<li>New Abrams designation if CITV is installed</li>
	<li>"TUSK" postfix when ERA is detected (NATO ERA v1.2.2+ required)</li>
	<li>M153 CROWS II option (Big thanks to Atlas!)</li>
	<li>Configurable CITV upgrade</li>
	<li>Configurable enhanced optics for the GPS, FLIR and GAS</li>
	<li>Configurable tank crew proficiency</li>
	<li>M2 Coaxial upgrade (replaces M240 and has two ammo types)</li>
	<li>Updated tank weight depending on the armor type used</li>
	<li>Configurable vehicle dynamics</li>
	<li>Toggleable Auxilliary Power Unit (APU)</li>
	<li>Configurable enhanced smoke launcher system</li>
	<li>Toggleable clean turret look (no attachments like ALICE packs or MREs)</li>
	<li>Toggleable IP model conversion for M1E1</li>
 	</ul>
</p>

## Installation:
1.) Install [MelonLoader v0.7.3](https://github.com/LavaGang/MelonLoader/).

2.) Download the latest version from the [release page](https://github.com/WpsxX/M1A1Abrams-AMP/releases).

3.) Place M1A1Abrams-AMP.dll, VanillaAssetUtil.dll and the m1a1amp_assets folder in the mods folder:
![AMP Installation](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/e3fe58f6-8968-4e89-8b98-37d94d6effe4)

4.) Launch the game directly (not from Steam).
   
5.) On first time running this mod, the entries in MelonPreferences.cfg will only appear after launching the game then normally closing it.


## NOTE!
<p>
	<ul> 
		<li>Only include ATLAS' Abrams mod .dll file in the mods folder or this one. <b>Do not place both .dll files at the same time.</b></li>
		<li>If you already have ATLAS' Abrams mod or have an older version of the Abrams AMP mod installed, either delete MelonPreferences.cfg in UserData folder or remove the lines pertaining to the [M1A1Config]/[M1A1AMPConfig] to make it easier to understand the custom config. Launch the game first then close it to update the contents of MelonPreferences.cfg</li>
		<li>Mission loading might be longer if you are using the HU variant, potentially due to the extra armor configuration.</li>
		<li>For compatibility with other mods for the TUSK upgrade and anti-ERA effects, ensure that the AMP mod is loaded after NATO ERA and Pact Increased Lethality mod.</li>
		<li>On the Gunner Auxiliary Sight (GAS) behavior: Only the first two rounds are considered and only one type of APFSDS (M829 series) and HEAT (other rounds) are considered. This means if you carry an all APFSDS/HEAT loadout but with different types, only the first round is considered by the GAS.</li>
	</ul>
</p>

## Round types list:
| Name  | Penetration (mm) | Fragment/Spalling Penetration (mm)| Muzzle Velocity (m/s) | Note |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| M829 APFSDS-T | 550 |  | 1670  | +15% spalling chance and +25% spalling performance. Optional +25% spalling chance instead. |
| M829A1 APFSDS-T  | 630 |  | 1575 | +20% spalling chance and +50% spalling performance. Optional +25% spalling chance instead. |
| M829A2 APFSDS-T | 680 |  | 1680 | +25% spalling chance and +100% spalling performance. ERA is only 90% effective. Optional +50% spalling chance instead. |
| M829A3 APFSDS-T | 750 |  | 1555 | +30% spalling chance and +150% spalling performance. ERA is only 20% effective. Optional +75% spalling chance instead. |
| M829A4 APFSDS-T | 1000 |  | 1700 | Hypothethical round. +100% spalling chance and ++200% spalling performance. ERA is only 15% effective.  |
| M830 HEAT-FS-T | 600 |  | 1140 | +50% spalling chance |
| M830A1 HEAT-MP-T | 480 | 50** | 1410 | Point-detonate fuze only. 600 fragments (configurable count). |
| M830A2 IHEAT-FS-T | 700 | 35** | 1410 | Fictional round. +100% spalling chance. ERA is only 15% effective. |
| M830A3 IHEAT-FS-T | 1000  | 25** | 1310 | Fictional round. +100% spalling chance. ERA is only 15% effective. |
| M908 HE-OR-T | 250* | 75** | 1410 | Has impact-delay fuze. 300 fragments (configurable count). |
| XM1147 AMP-T | 250* | 120** | 1410 | Point-detonate + airburst fuze or proximity fuze. 600 fragments (configurable count). Has impact-delay fuze.|
| LAHAT | 800 | 15** | 300 | Gun-launched ATGM with SALH guidance. +100% spalling chance. ERA is only 20% effective. |
| M2/M8 AP-T/I | 29 | - | 887 | M2 Coax / CROWS |
| M962 SLAP-T | 36 | - | 1200 | M2 Coax / CROWS. Has slightly less drag than M2/M8. |

- After discussions with other players and researching, the penetration values for the M829 series have been revamped except for M829A4
- M829A4 simply has made up stats
- There is still a config option to reuse [Steel Beasts values](https://www.steelbeasts.com/sbwiki/index.php/Ammunition_Data#Main_Gun) if desired and extra spalling chance
- Values for Anti-ERA effects are a total guess
- *These are HE rounds so actual penetration is not 250mm
- **These are <i>up to</i> values so not every fragment will perform the same
- M830A1, XM1147, M908 have slightly less drag

## How to use the XM1147 AMP-T or M830A1 HEAT-MP-T:
### Point-detonate + Time-delay Fuze (AMP only)

- Default fuzing type for AMP
- To use airburst mode, lase the target and aim slightly above it so that the shell will not impact the target. The airburst fuzing is not perfect since it might explode in front or behind the target instead of somewhere in the middle, so multiple shots might be required. Or to help compensate for the deviation, increase the fragment count but this can impact game performance.
- To use point-detonate mode, make sure the range setting is at least 10 meters more than the target to ensure it would not be in airburst mode. As long as the shell directly hits the target, it will use the point-detonate fuze.


### Point-detonate + Proxmity Fuze
- Proximity fuze option automatically enabled for the MPAT, but needs to be enabled in the config for the AMP 
- To use proximity mode, press middle mouse button and the round should have [Proximity] postfix to its name in the lower left part of the UI
- To use point-detonate mode, make sure the [Proximity] postfix is not present

![AMP Proximity Mode](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/e665ec03-62bd-44fe-9276-4f1d7ab12035)		

## Armor variants list:
### Heavy Armor (HA) - roughly 30% increase in protection
| Area  | Protection vs KE (mm) | Protection vs CE (mm) 
| ------------- | ------------- | ------------- | 
| Hull front | 530  | 1070  |
| Turret cheek | 620 | 1350 | 
| Turret side | 500 | 460 | 
| Composite side skirt | 95 | 130 | 
| Gun mantlet | 500 | 800 | 

### Heavy Common (HC) - roughly 45% increase in protection
| Area  | Protection vs KE (mm) | Protection vs CE (mm) 
| ------------- | ------------- | ------------- | 
| Hull front | 590  | 1200  |
| Turret cheek | 690 | 1510 | 
| Turret side | 510 | 560 | 
| Composite side skirt | 105 | 145 | 
| Gun mantlet | 540 | 820 | 

### Situational Awareness (SA) - roughly 85% increase in KE protection
| Area  | Protection vs KE (mm) | Protection vs CE (mm) | Note
| ------------- | ------------- | ------------- | ------------- |
| Hull front | 750  | 1200  |  |
| Turret cheek | 910 | 1510 |   |
| Turret side | 510 | 560 |   |
| Composite side skirt | 105 | 145 |   |
| Gun mantlet | 540 | 820 |   |
<p>
	<ul> 
		<li>Only the turret cheeks and hull front had a KE protection upgrade for the SA variant</li>
		<li>The other areas have the same values as the HC variant</li>
	</ul>
</p>

#### Lightweight (L)
| Area  | Protection vs KE (mm) | Protection vs CE (mm) | Note
| ------------- | ------------- | ------------- | ------------- |
| Turret cheek | 480 | 1040 |   |
| Composite side skirt | 47 | 75 |   |
| Gun mantlet | 455 | 735 |   |

<p>
	<ul> 
		<li>Base M1A1 protection but no hull or turret side special armor array</li>
	</ul>
</p>

### Heavy Ultra (HU) - Custom protection values
| Area  | Protection vs KE (mm) | Protection vs CE (mm) | Note
| ------------- | ------------- | ------------- | ------------- | 
| Hull front | 1030  | 1500  |  |
| Hull side/roof/bottom/rear | 200 | 300 | |
| Hull firewall | 30 | 30 | Acts as spall liner |
| Engine deck | 250 | 350 | |
| Turret cheek | 1130 | 1800 |  |
| Turret side | 730 | 1240 |  |
| Turret rear | 500 | 1000 | |
| Turret firewall | 30 | 30 | Acts as spall liner |
| Composite side skirt | 300 | 550 | |
| Side skirt | 300 | 550 | The non-composite ones |
| Gun mantlet | 660 | 1020 | |
| Gun mantlet face | 240 | 300 | |
| Upper glacis | 180 | 210 | |
| Turret ring | 180 | 210 | |
| Turret roof | 150 | 180 | |
| Commander's hatch | 140 | 175 | |
| Loader's hatch | 140 | 175 | |
| Driver's hatch | 140 | 175 | |
| Hull ammo rack door | 45 | 45 | |
<p>
	<ul> 
		<li>Armor testing done in game which can result in inconsistency between the area shot at, and the angle and distance from it. However, I did my best to shoot at less than 100 meters and as close to 0 degrees for the perpendicular shot to the armor face.</li>
		<li>The only exception is the turret cheek and hull front where I emulated a headon engagement.</li>
		<li>HA and HC armor figures are roughly a 30% and 45% increase in protection becaue finding proper documentation is difficult (please don't violate ITAR). However, the values can be changed in the future if required. Although if you believe you found a seemingly decent (and unclass) source for the values, please let me know.</li>
		<li>If the protection increase seem to be arbitrary, I'm basing the values relative to the available (REDFOR) vanilla and modded penetrators. Example would be HA's cheek array <i>might</i> be able to hold against the T-64B mod 3BM42 (660mm which is highest REDFOR AP pen value) while the HC would completely protect against it.</li>
		<li>HU armor figures are based on Steal Beasts Abrams and my own inputs. It's basically the Abrams that received armor improvements everywhere (BEGONE SWaP-C).</li>
	</ul>
</p>

## Abrams Renaming 
<p>
	<ul> 
		<li>Dependent on CITV installation only</li>
		<li>M1E1 also gets same renaming scheme as the M1A1</li>
	</ul>
</p>

| Standard | w/ CITV | Note |
| ------------- | ------------- | ------------- |
| M1A1HA | M1A1 AIM |  | 
| M1A1HC | M1A2 |  | 
| M1A1SA | M1A2 SEP |  | 
| M1A1HU | M1A2U |  | 
| M1A1L | M1A2L |  | 

## M153 CROWS II
<p>
	<ul> 
		<li>Removes Commander's HMG and replaces it with M153 CROWS II</li>
		<li>Option of two ammo types (same as M2 Coax)</li>
		<li>400 ready rounds</li>
		<li>Toggleable upgrade per tank</li>
		<li>Toggleable CROWS position per tank</li>
		<li>MK19 CROWS option (Mk 19 automatic grenade launcher)</li>
	</ul>
</p>

## Vehicle Dynamics
### Weight
| Armor Type | Weight (KG) | Note |
| ------------- | ------------- | ------------- |
| Base | 59,057 | Base M1A1/E1 weight | 
| L | 44,838 |  | 
| HA | 60,599 |  | 
| HC | 61,416 |  | 
| SA | 62,232 |  | 
| HU* | 72,665 | Fully loaded SEPv3 used as reference | 
<p>
	<ul> 
		<li>*HU variant has optional Unobtanium armor package (UAP) that lets it weigh the same as the HA</li>
	</ul>
</p>

### Engines
| Engine type  | Peak power (HP) | Peak Torque (NM) | Max RPM | Braking Power | Note |
| ------------- | ------------- | ------------- | ------------- | ------------- | ------------- | 
| AGT1500 | 1500 | 5741 | 3100 |  | Default engine. | 
| AGT2000 | 2000 | 7655 | 3100 | +10% | Fictional. | 
| AGT2500 | 2500 | 7415 | 4000 | +20% | Fictional. | 
| AGT3000 | 3000 | 8899 | 4000 | +30% | Fictional. | 
| T64 | 4430 | 12225 | 4300 | +40% | Fictional but it's the General Electric T64-GE-100. | 
| T55 | 6000 | 14833 | 4800 | +50% | Fictional but it's the Honeywell T55-L-714C. | 


### Transmission
<p>
	<ul> 
		<li>Upgradeable transmission with 6 forward gears and 3 reverse gears (compared to 4/2 of default)</li>
	</ul>
</p>

### Suspension
<p>
	<ul> 
		<li>Added upgradeable suspension which slightly increases ride height, and increases suspension travel that has better performance on rough terrain </li>
	</ul>
</p>


### Tracks
<p>
	<ul> 
		<li>Added upgradeable tracks which increases grip to aid acceleration, braking and movement on steep terrain</li>
	</ul>
</p>


### Governor
<p>
	<ul> 
		<li>Governor delete option which raises potential top speed to 129 km/h forwards and 72 km/h backwards (compared to 72 km/h and 40 km/h with governor)</li>
	</ul>
</p>

### Stability Control (SC)
<p>
	<ul> 
		<li>Added stability control option which makes the tank more controllable at higher speeds</li>
	</ul>
</p>

## Other Features
### Enhanced optics
<p>
	<ul> 
		<li>GPS has 5 zoom levels (vs 2 for vanilla)</li>
		<li>Thermals have 5 zoom levels (vs 2 for vanilla), clearer image and removed scanlines</li>
		<li>GAS has 4 zoom levels (vs 1 for vanilla) and its less blurry when moving</li>
	</ul>
</p>

### Auxilliary Power Unit (APU)
<p>
	<ul> 
		<li>Retain nomral turret traversal speed (40°/s) and lazing functionality when engine is destroyed</li>
		<li>Toggle for faster turret traversal speed (60°/s) if the engine is T64 and if it's still running</li>
		<li>The APU itself has no model at the moment so it's indestructible</li>
	</ul>
</p>

### Smoke Launcher Upgrade
<p>
	<ul> 
		<li>Smoke+ upgrade, doubling the amount of salvos that could be fired and slightly increasing the throw distace of smoke grenades</li>
		<li>Smoke+ upgrade also lets any crew activate the smoke grenades/generator instead of being tied to a specific member</li>
		<li>ROSY upgrade, featuring 4 salvos and 12 smoke grenades fired per salvo covering a +/- 82° sector in front of the turret</li>
		<li>Option for improved ROSY* that has a faster deployment time and greater smoke generation, and even multispectral capability</li>
		<li>*Due to challenges in modding smoke grenades, the <b>vanilla smoke grenade is directly modified</b> which means the option above will also affect the smoke grenade behavior of the M60A1/3, M2, M113 and unupgraded M1 smoke system.</li>
		<li>*FPS drops might occur from the enhanced smoke effects</li>
	</ul>
</p>

### Crew Proficiency
<p>
	<ul> 
		<li>Commander and Gunner can have better (or worse) target spotting and gunnery skill</li>
		<li>Loader skill can have a loading time of 7 seconds (min qualification) and there is 1 second reduction as you go up in skill</li>
		<li>Out of action loader and ready rack loading times are also affected</li>
		<li>Has 4 skill levels, with Regular being default and the same as vanilla behavior (or slightly better)</li>
	</ul>
</p>

## Mod Configuration (in UserData/MelonPreferences.cfg):

<p>
	<ul> 
		<li>I suggest getting Notepad++ so it would be easier to identify each category</li>
		<li>Selectable round types for four slots for M1A1 and M1E1. You can even go with a pure APFSDS or HEAT loadout. <b>Be careful when typing the round you want </b> because the mod will default to vanilla rounds (M833/M456A2) if there is a typo or if a field is left blank. The listed rounds are case-sensitive (just make sure it is all caps when entering the designated round).</li>
		<li>Customize how many rounds per type are carried by the M1A1 and M1E1 (total max of 50).</li>
		<li>Customize the tank gunner, commander and loader skill per tank (Regular by default)</li>
		<li>Upgrade the M240 coax to M2 coax with two ammo options (false by default)</li>
		<li>Enhanced M829 spalling (false by default)</li>
		<li>Customize the number of fragments generated by the M830A1 (600 by default). <b>Be careful when setting a higher number</b> because it can negatively affect game performance.</li>
		<li>Customize the number of fragments generated by the XM1147 (600 by default). <b>Be careful when setting a higher number</b> because it can negatively affect game performance.</li>
		<li>Customize the number of fragments generated by the M908 (300 by default). <b>Be careful when setting a higher number</b> because it can negatively affect game performance.</li>
		<li>XM1147 fuze type betweeen point-detonate + time-delay (default) or point-detonate + proximity</li>
		<li>Trigger distance for proximity-enabled rounds (3 by default)</li>
		<li>Enhanced gunner optics for Daysight, Thermals and GAS (false by default)</li>
		<li>Enable the CITV and customize its behavior (false by default)</li>
 		<li>Horizontal sight stabilization for M1A1/E1s when applying lead (false by default)</li>
		<li>Convert non-IP M1s to M1E1s (true by default)</li>
		<li>Randomize M1A1/E1 conversions (true by default) </li>
		<li>Customize the armor variant used by M1A1 and M1E1 (SA for M1A1 and HC for M1E1 by default). M1 must be converted to M1E1 to allow armor conversions. The mod will default to vanilla base armor when there is a typo or if a field is left blank.</li>
		<li>UAP option to make the HU armor weigh the same as the HA (false by default)</li>
		<li>Demigod armor for the HU variant if you want an almost unkillable Abrooms (false by default)</li>
		<li>Enhanced smoke launching system (false by default)</li>
		<li>ROSY upgrade (false by default)</li>
		<li>Improved ROSY (false by default)</li>
		<li>Cuztomize the engine per tank (AGT1500 by default)</li>
		<li>Enhanced transmission (false by default)</li>
		<li>Enhanced suspension (false by default)</li>
		<li>Enhanced tracks (false by default)</li>
		<li>Engine governor removal (false by default)</li>
		<li>Stability Control (false by default)</li>
		<li>Enable APU per tank (false by default)</li>
		<li>Enable bonus turret traversal with APU installed (false by default)</li>
		<li>Remove turret decors like ALICE packs or MRE boxes (false by default)</li>
		<li>M1E1 uses M1IP model (false by default)</li>
		<li>Use M256 Model (true by default)</li>
		<li>Forced camo net appearance per tank (false by default)</li>
	</ul>
</p>

![AMP MelonPreferences v3-1](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/13417ec3-b701-4c5e-aba9-3915770960ff)
![AMP MelonPreferences v3-2](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/a4f0a666-3852-419f-8bfe-778b61fa4d1b)

## 简体中文

首先非常感谢 ATLAS/thebeninator 最初提供了艾布拉姆斯与布拉德利 50mm mod！所需的全部代码都来自他的 mod，我只是想办法把这些部分拼装起来，从而复刻出 AMP 弹种。同时也要感谢 Swiss/SovGrenadier 提供的 GLATGM 实现。请务必去看看他们的 mod，非常出色！

《Gunner, HEAT, PC!》的 mod。需要 [MelonLoader](https://github.com/LavaGang/MelonLoader/)

如果通过 Steam 启动时报错，需要直接运行游戏的可执行文件。

![Screenshot_1](https://raw.githubusercontent.com/Cyances/M1A1AbramsAMP/master/Images/AMP%20vs%20SPW-60B.png)
![Screenshot_3](https://raw.githubusercontent.com/Cyances/M1A1AbramsAMP/master/Images/AMP%20vs%20T-55A.png)
![Screenshot_4](https://raw.githubusercontent.com/Cyances/M1A1AbramsAMP/master/Images/AMP%20vs%20T-55A%20XRay.png)
![AMP vs Mi-24](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/2727aa85-097b-41c6-8a31-6a6c8dd7af12)
![AMP Enhanced Optics v2](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/c538fba0-c967-429f-b91a-e6d338ec7a4e)

## AMP 2.7.6 更新内容：
<p>
	<ul>
	<li>适配游戏版本 20261014</li>
	<li>新增 LAHAT 攻顶模式：中键切换攻顶/直射，锁定与未锁定发射都会走“爬升—平飞—俯冲”剖面</li>
	<li>修复 CROWS，并新增 MK19 CROWS</li>
	<li>120mm M256 火炮（含全新模型！）</li>
	<li>M1IP 自动转换为 M1A1</li>
	<li>基于现实发展（数值为估算）与假设性构想设计的坦克升级</li>
     	<li>可配置的艾布拉姆斯装甲型号：基础装甲、重装甲 HA（防护 +30%）、重型通用 HC（防护 +45%）、态势感知 SA（+85%）以及重型终极 HU（自定义）</li>
     	<li>可配置弹种（最多四个挂位，总携弹量上限 50 发）</li>
	<li>M1A1 默认载弹：20 发 M829A4、12 发 M830A2、12 发 XM1147、6 发 LAHAT</li>
	<li>M1E1 默认载弹：20 发 M829、15 发 M830、15 发 M830A1（按 2/3 键选择 M830/A1 [4 号位只是一个空的 M830A1 弹架]）</li>
	<li>APFSDS：M829、M829A1、M829A2、M829A3、M829A4</li>
    	<li>HEAT：M830、M830A1、M830A2、M830A3、XM1147</li>
    	<li>HE：XM1147、M908</li>
    	<li>GLATGM：LAHAT</li>
    	<li>XM1147 AMP-T 多用途引信弹（触发 + 延时或近炸）</li>
	<li>AP 与 HEAT 具备反反应装甲/串联战斗部特性（针对 (Super) Kontakt-1、Kontakt-5、ARAT 与 BRAT 建模），至少需要 NATO ERA v1.1.1b 和/或 Pact Increased Lethality v1.2.5+</li>
	<li>安装 CITV 后采用新的艾布拉姆斯型号命名</li>
	<li>检测到反应装甲时附加“TUSK”后缀（需要 NATO ERA v1.2.2+）</li>
	<li>可选 M153 CROWS II（非常感谢 Atlas！）</li>
	<li>可配置 CITV 升级</li>
	<li>可为 GPS、FLIR 与 GAS 配置增强光学</li>
	<li>可配置车组熟练度</li>
	<li>M2 并列机枪升级（替换 M240，有两种弹种）</li>
	<li>根据所用装甲型号更新坦克重量</li>
	<li>可配置车辆动力学</li>
	<li>可开关的辅助动力装置（APU）</li>
	<li>可配置的增强烟雾弹发射系统</li>
	<li>可开关的整洁炮塔外观（去除 ALICE 背包或 MRE 口粮等附件）</li>
	<li>可开关 M1E1 的 IP 模型转换</li>
 	</ul>
</p>

## 安装：
1.) 安装 [MelonLoader v0.7.3](https://github.com/LavaGang/MelonLoader/)。

2.) 从[发布页面](https://github.com/WpsxX/M1A1Abrams-AMP/releases)下载最新版本。

3.) 将 M1A1Abrams-AMP.dll、VanillaAssetUtil.dll 以及 m1a1amp_assets 文件夹放入 mods 文件夹：
![AMP Installation](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/e3fe58f6-8968-4e89-8b98-37d94d6effe4)

4.) 直接启动游戏（不要通过 Steam）。
   
5.) 首次运行本 mod 时，MelonPreferences.cfg 中的条目只有在你启动游戏并正常关闭之后才会出现。


## 注意！
<p>
	<ul> 
		<li>mods 文件夹中只能放入 ATLAS 的艾布拉姆斯 mod 的 .dll 文件，或者本 mod 的 .dll 文件。<b>请勿同时放置这两个 .dll 文件。</b></li>
		<li>如果你已经安装了 ATLAS 的艾布拉姆斯 mod，或装有旧版本的 Abrams AMP mod，请删除 UserData 文件夹中的 MelonPreferences.cfg，或移除与 [M1A1Config]/[M1A1AMPConfig] 相关的行，以便更容易理解自定义配置。请先启动游戏再关闭，以更新 MelonPreferences.cfg 的内容</li>
		<li>使用 HU 型号时任务加载时间可能更长，可能是因为额外的装甲配置。</li>
		<li>为使 TUSK 升级与反反应装甲效果与其他 mod 兼容，请确保 AMP mod 在 NATO ERA 和 Pact Increased Lethality mod 之后加载。</li>
		<li>关于炮手辅助瞄具（GAS）的行为：只考虑前两种弹，且只区分一种 APFSDS（M829 系列）和 HEAT（其他弹种）。这意味着如果你携带的是全 APFSDS/HEAT 载弹但型号不同，GAS 只会考虑第一种弹。</li>
	</ul>
</p>

## 弹种列表：
| 名称 | 穿深（mm） | 破片/崩落穿深（mm）| 初速（m/s） | 备注 |
| ------------- | ------------- | ------------- | ------------- | ------------- |
| M829 APFSDS-T | 550 |  | 1670  | +15% 崩落概率与 +25% 崩落效能。也可选 +25% 崩落概率。 |
| M829A1 APFSDS-T  | 630 |  | 1575 | +20% 崩落概率与 +50% 崩落效能。也可选 +25% 崩落概率。 |
| M829A2 APFSDS-T | 680 |  | 1680 | +25% 崩落概率与 +100% 崩落效能。反应装甲仅有 90% 效能。也可选 +50% 崩落概率。 |
| M829A3 APFSDS-T | 750 |  | 1555 | +30% 崩落概率与 +150% 崩落效能。反应装甲仅有 20% 效能。也可选 +75% 崩落概率。 |
| M829A4 APFSDS-T | 1000 |  | 1700 | 假想弹种。+100% 崩落概率与 ++200% 崩落效能。反应装甲仅有 15% 效能。  |
| M830 HEAT-FS-T | 600 |  | 1140 | +50% 崩落概率 |
| M830A1 HEAT-MP-T | 480 | 50** | 1410 | 仅触发引信。600 枚破片（数量可配置）。 |
| M830A2 IHEAT-FS-T | 700 | 35** | 1410 | 虚构弹种。+100% 崩落概率。反应装甲仅有 15% 效能。 |
| M830A3 IHEAT-FS-T | 1000  | 25** | 1310 | 虚构弹种。+100% 崩落概率。反应装甲仅有 15% 效能。 |
| M908 HE-OR-T | 250* | 75** | 1410 | 具备着发延时引信。300 枚破片（数量可配置）。 |
| XM1147 AMP-T | 250* | 120** | 1410 | 触发 + 空爆引信或近炸引信。600 枚破片（数量可配置）。具备着发延时引信。|
| LAHAT | 800 | 15** | 300 | 炮射反坦克导弹，采用半主动激光寻的（SALH）制导。+100% 崩落概率。反应装甲仅有 20% 效能。 |
| M2/M8 AP-T/I | 29 | - | 887 | M2 并列机枪 / CROWS |
| M962 SLAP-T | 36 | - | 1200 | M2 并列机枪 / CROWS。阻力略低于 M2/M8。 |

- 在与其他玩家讨论并研究后，M829 系列的穿深数值已重新调整（M829A4 除外）
- M829A4 的数值纯属虚构
- 如果愿意，配置中仍可选择沿用 [Steel Beasts 数值](https://www.steelbeasts.com/sbwiki/index.php/Ammunition_Data#Main_Gun) 以及额外崩落概率
- 反反应装甲效果的数值完全是猜测
- *这些是榴弹，因此实际穿深并非 250mm
- **这些是<i>最高</i>数值，因此并非每枚破片的表现都相同
- M830A1、XM1147、M908 的阻力略低

## 如何使用 XM1147 AMP-T 或 M830A1 HEAT-MP-T：
### 触发 + 延时引信（仅 AMP）

- AMP 的默认引信类型
- 要使用空爆模式，请对目标进行激光测距，并瞄准目标稍上方，使炮弹不会命中目标。空爆引信并不完美，因为它可能在目标前方或后方爆炸，而不是在中间某个位置，因此可能需要多次射击。或者为了补偿这种偏差，可以增加破片数量，但这可能影响游戏性能。
- 要使用触发模式，请确保装定距离比目标至少多 10 米，以保证不会进入空爆模式。只要炮弹直接命中目标，就会使用触发引信。


### 触发 + 近炸引信
- MPAT 会自动启用近炸引信选项，但 AMP 需要在配置中启用
- 要使用近炸模式，按下鼠标中键，此时 UI 左下角的弹种名称后应带有 [Proximity] 后缀
- 要使用触发模式，请确保不带有 [Proximity] 后缀

![AMP Proximity Mode](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/e665ec03-62bd-44fe-9276-4f1d7ab12035)		

## 装甲型号列表：
### 重装甲（HA）——防护约提升 30%
| 区域 | 抗动能弹防护（mm） | 抗化学能弹防护（mm） 
| ------------- | ------------- | ------------- | 
| 车体前部 | 530  | 1070  |
| 炮塔颊部 | 620 | 1350 | 
| 炮塔侧面 | 500 | 460 | 
| 复合侧裙 | 95 | 130 | 
| 炮盾 | 500 | 800 | 

### 重型通用（HC）——防护约提升 45%
| 区域 | 抗动能弹防护（mm） | 抗化学能弹防护（mm） 
| ------------- | ------------- | ------------- | 
| 车体前部 | 590  | 1200  |
| 炮塔颊部 | 690 | 1510 | 
| 炮塔侧面 | 510 | 560 | 
| 复合侧裙 | 105 | 145 | 
| 炮盾 | 540 | 820 | 

### 态势感知（SA）——抗动能防护约提升 85%
| 区域 | 抗动能弹防护（mm） | 抗化学能弹防护（mm） | 备注
| ------------- | ------------- | ------------- | ------------- |
| 车体前部 | 750  | 1200  |  |
| 炮塔颊部 | 910 | 1510 |   |
| 炮塔侧面 | 510 | 560 |   |
| 复合侧裙 | 105 | 145 |   |
| 炮盾 | 540 | 820 |   |
<p>
	<ul> 
		<li>SA 型号只有炮塔颊部与车体前部获得了抗动能防护升级</li>
		<li>其他区域的数值与 HC 型号相同</li>
	</ul>
</p>

#### 轻量化（L）
| 区域 | 抗动能弹防护（mm） | 抗化学能弹防护（mm） | 备注
| ------------- | ------------- | ------------- | ------------- |
| 炮塔颊部 | 480 | 1040 |   |
| 复合侧裙 | 47 | 75 |   |
| 炮盾 | 455 | 735 |   |

<p>
	<ul> 
		<li>基础 M1A1 防护水平，但没有车体或炮塔侧面的特种装甲阵列</li>
	</ul>
</p>

### 重型终极（HU）——自定义防护数值
| 区域 | 抗动能弹防护（mm） | 抗化学能弹防护（mm） | 备注
| ------------- | ------------- | ------------- | ------------- | 
| 车体前部 | 1030  | 1500  |  |
| 车体侧面/顶部/底部/后部 | 200 | 300 | |
| 车体防火墙 | 30 | 30 | 作为崩落衬层 |
| 发动机舱盖 | 250 | 350 | |
| 炮塔颊部 | 1130 | 1800 |  |
| 炮塔侧面 | 730 | 1240 |  |
| 炮塔后部 | 500 | 1000 | |
| 炮塔防火墙 | 30 | 30 | 作为崩落衬层 |
| 复合侧裙 | 300 | 550 | |
| 侧裙 | 300 | 550 | 非复合材质的那种 |
| 炮盾 | 660 | 1020 | |
| 炮盾正面 | 240 | 300 | |
| 首上装甲 | 180 | 210 | |
| 炮塔环 | 180 | 210 | |
| 炮塔顶部 | 150 | 180 | |
| 车长舱盖 | 140 | 175 | |
| 装填手舱盖 | 140 | 175 | |
| 驾驶员舱盖 | 140 | 175 | |
| 车体弹药架舱门 | 45 | 45 | |
<p>
	<ul> 
		<li>装甲测试是在游戏内进行的，因此射击部位与其角度、距离之间可能存在不一致。不过我已尽量在 100 米以内、接近 0 度垂直入射装甲面的条件下射击。</li>
		<li>唯一的例外是炮塔颊部与车体前部，这两处我模拟的是正面交战。</li>
		<li>HA 与 HC 的装甲数值大致对应防护提升 30% 与 45%，因为找到合适的资料很困难（请不要违反 ITAR）。不过如有需要，这些数值日后仍可调整。如果你认为自己找到了看起来还不错（且非密）的数值来源，请告诉我。</li>
		<li>如果这些防护提升看起来有些随意，我是以现有的（红方）原版与 mod 穿甲弹为参照来确定数值的。例如 HA 的颊部阵列<i>也许</i>能挡住 T-64B mod 的 3BM42（660mm，红方 AP 最高穿深），而 HC 则能完全防护。</li>
		<li>HU 的装甲数值基于 Steel Beasts 的艾布拉姆斯以及我自己的输入。基本上就是全方位都做了装甲强化的艾布拉姆斯（SWaP-C 走开）。</li>
	</ul>
</p>

## 艾布拉姆斯重命名 
<p>
	<ul> 
		<li>仅取决于是否安装 CITV</li>
		<li>M1E1 也使用与 M1A1 相同的重命名方案</li>
	</ul>
</p>

| 标准型号 | 配备 CITV | 备注 |
| ------------- | ------------- | ------------- |
| M1A1HA | M1A1 AIM |  | 
| M1A1HC | M1A2 |  | 
| M1A1SA | M1A2 SEP |  | 
| M1A1HU | M1A2U |  | 
| M1A1L | M1A2L |  | 

## M153 CROWS II
<p>
	<ul> 
		<li>移除车长高射机枪，替换为 M153 CROWS II</li>
		<li>可选两种弹种（与 M2 并列机枪相同）</li>
		<li>400 发待发弹</li>
		<li>按坦克开关该升级</li>
		<li>按坦克开关 CROWS 位置</li>
		<li>可选 MK19 CROWS（Mk 19 自动榴弹发射器）</li>
	</ul>
</p>

## 车辆动力学
### 重量
| 装甲型号 | 重量（KG） | 备注 |
| ------------- | ------------- | ------------- |
| 基础 | 59,057 | M1A1/E1 基础重量 | 
| L | 44,838 |  | 
| HA | 60,599 |  | 
| HC | 61,416 |  | 
| SA | 62,232 |  | 
| HU* | 72,665 | 以满载 SEPv3 作为参考 | 
<p>
	<ul> 
		<li>*HU 型号可选“Unobtanium 装甲包”（UAP），使其重量与 HA 相同</li>
	</ul>
</p>

### 发动机
| 发动机型号 | 峰值功率（HP） | 峰值扭矩（NM） | 最大转速 | 制动功率 | 备注 |
| ------------- | ------------- | ------------- | ------------- | ------------- | ------------- | 
| AGT1500 | 1500 | 5741 | 3100 |  | 默认发动机。 | 
| AGT2000 | 2000 | 7655 | 3100 | +10% | 虚构。 | 
| AGT2500 | 2500 | 7415 | 4000 | +20% | 虚构。 | 
| AGT3000 | 3000 | 8899 | 4000 | +30% | 虚构。 | 
| T64 | 4430 | 12225 | 4300 | +40% | 虚构，但实为 General Electric T64-GE-100。 | 
| T55 | 6000 | 14833 | 4800 | +50% | 虚构，但实为 Honeywell T55-L-714C。 | 


### 传动
<p>
	<ul> 
		<li>可升级传动，具备 6 个前进挡与 3 个倒挡（默认为 4/2）</li>
	</ul>
</p>

### 悬挂
<p>
	<ul> 
		<li>新增可升级悬挂，略微提升车高，并增加悬挂行程，在崎岖地形上表现更好 </li>
	</ul>
</p>


### 履带
<p>
	<ul> 
		<li>新增可升级履带，提升抓地力，有助于加速、制动以及在陡峭地形上行驶</li>
	</ul>
</p>


### 限速器
<p>
	<ul> 
		<li>可选拆除限速器，将潜在最高速度提升至前进 129 km/h、倒车 72 km/h（有限速器时为 72 km/h 与 40 km/h）</li>
	</ul>
</p>

### 稳定控制（SC）
<p>
	<ul> 
		<li>新增稳定控制选项，使坦克在较高速度下更易操控</li>
	</ul>
</p>

## 其他特性
### 增强光学
<p>
	<ul> 
		<li>GPS 具备 5 级倍率（原版为 2 级）</li>
		<li>热成像具备 5 级倍率（原版为 2 级），图像更清晰并去除扫描线</li>
		<li>GAS 具备 4 级倍率（原版为 1 级），移动时也不那么模糊</li>
	</ul>
</p>

### 辅助动力装置（APU）
<p>
	<ul> 
		<li>发动机被击毁后仍保持正常炮塔转速（40°/s）与激光测距功能</li>
		<li>若发动机为 T64 且仍在运转，可切换更快的炮塔转速（60°/s）</li>
		<li>APU 目前没有模型，因此无法被摧毁</li>
	</ul>
</p>

### 烟雾弹发射器升级
<p>
	<ul> 
		<li>Smoke+ 升级，可发射的齐射次数翻倍，并略微增加烟雾弹的投掷距离</li>
		<li>Smoke+ 升级还允许任意车组成员激活烟雾弹/烟雾发生器，而不再绑定特定成员</li>
		<li>ROSY 升级，具备 4 次齐射、每次齐射发射 12 枚烟雾弹，覆盖炮塔前方 ±82° 扇区</li>
		<li>可选改进型 ROSY*，部署时间更快、发烟量更大，甚至具备多光谱能力</li>
		<li>*由于烟雾弹的 mod 制作存在困难，<b>原版烟雾弹被直接修改</b>，这意味着上述选项也会影响 M60A1/3、M2、M113 以及未升级 M1 烟雾系统的烟雾弹行为。</li>
		<li>*增强的烟雾效果可能导致帧率下降</li>
	</ul>
</p>

### 车组熟练度
<p>
	<ul> 
		<li>车长与炮手可以拥有更好（或更差）的目标发现与射击技能</li>
		<li>装填手技能最低资格对应 7 秒装填时间，技能每提升一级减少 1 秒</li>
		<li>装填手失能与待发弹架装填时间也会受到影响</li>
		<li>共有 4 个技能等级，Regular 为默认，与原版行为相同（或略好）</li>
	</ul>
</p>

## Mod 配置（位于 UserData/MelonPreferences.cfg）：

<p>
	<ul> 
		<li>建议安装 Notepad++，这样更容易分辨各个分类</li>
		<li>M1A1 与 M1E1 的四个挂位均可选择弹种，甚至可以搭配纯 APFSDS 或纯 HEAT 载弹。<b>输入想要的弹种时要小心</b>，因为一旦拼写错误或字段留空，mod 就会回退到原版弹种（M833/M456A2）。列出的弹种区分大小写（输入时请确保全部大写）。</li>
		<li>自定义 M1A1 与 M1E1 各弹种的携弹量（总上限 50 发）。</li>
		<li>按坦克自定义炮手、车长与装填手技能（默认为 Regular）</li>
		<li>将 M240 并列机枪升级为 M2 并列机枪，具备两种弹药选项（默认 false）</li>
		<li>增强 M829 崩落效果（默认 false）</li>
		<li>自定义 M830A1 产生的破片数量（默认 600）。<b>设置较高数值时要小心</b>，因为它可能对游戏性能造成负面影响。</li>
		<li>自定义 XM1147 产生的破片数量（默认 600）。<b>设置较高数值时要小心</b>，因为它可能对游戏性能造成负面影响。</li>
		<li>自定义 M908 产生的破片数量（默认 300）。<b>设置较高数值时要小心</b>，因为它可能对游戏性能造成负面影响。</li>
		<li>XM1147 引信类型：触发 + 延时（默认）或触发 + 近炸</li>
		<li>近炸弹的触发距离（默认 3）</li>
		<li>为昼间瞄准镜、热成像与 GAS 启用增强炮手光学（默认 false）</li>
		<li>启用 CITV 并自定义其行为（默认 false）</li>
 		<li>M1A1/E1 在提前量瞄准时启用水平瞄准线稳定（默认 false）</li>
		<li>将非 IP 的 M1 转换为 M1E1（默认 true）</li>
		<li>随机化 M1A1/E1 转换（默认 true） </li>
		<li>自定义 M1A1 与 M1E1 使用的装甲型号（M1A1 默认 SA，M1E1 默认 HC）。M1 必须先转换为 M1E1 才能进行装甲转换。若拼写错误或字段留空，mod 会回退到原版基础装甲。</li>
		<li>UAP 选项，使 HU 装甲重量与 HA 相同（默认 false）</li>
		<li>为 HU 型号启用“半神”装甲，让你拥有几乎无法被击毁的 Abrooms（默认 false）</li>
		<li>增强烟雾弹发射系统（默认 false）</li>
		<li>ROSY 升级（默认 false）</li>
		<li>改进型 ROSY（默认 false）</li>
		<li>按坦克自定义发动机（默认 AGT1500）</li>
		<li>增强传动（默认 false）</li>
		<li>增强悬挂（默认 false）</li>
		<li>增强履带（默认 false）</li>
		<li>拆除发动机限速器（默认 false）</li>
		<li>稳定控制（默认 false）</li>
		<li>按坦克启用 APU（默认 false）</li>
		<li>安装 APU 时启用炮塔转速加成（默认 false）</li>
		<li>移除炮塔装饰，如 ALICE 背包或 MRE 箱（默认 false）</li>
		<li>M1E1 使用 M1IP 模型（默认 false）</li>
		<li>使用 M256 模型（默认 true）</li>
		<li>按坦克强制使用伪装网外观（默认 false）</li>
	</ul>
</p>

![AMP MelonPreferences v3-1](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/13417ec3-b701-4c5e-aba9-3915770960ff)
![AMP MelonPreferences v3-2](https://github.com/Cyances/M1A1AbramsAMP/assets/154455050/a4f0a666-3852-419f-8bfe-778b61fa4d1b)
