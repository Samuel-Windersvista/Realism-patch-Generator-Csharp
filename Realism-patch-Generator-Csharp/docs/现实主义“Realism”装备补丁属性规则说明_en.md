# Realism Output Field Boundary Specification

This document defines the accepted Realism-standard output field boundary and explains the role of fields in final patches.

## 1. Purpose

This document answers:

- Which fields are Realism-standard patch fields
- What each field does in final output

Hard boundary in current repository:

- A field appearing in input JSON does not automatically mean it is allowed in output
- Fields such as TemplateID, itemTplToClone, ItemToClone, overrideProperties, Prefab, traderItems, barterScheme, SingleFireRate, Cartridges, and Slots may appear in source mod input
- Final output can only keep Realism-standard patch fields
- Allowed output fields are determined by default templates, rule allowlists, and modType-specific field mappings in code

## 2. Where Field Boundaries Come From

Standard output fields are determined by:

1. default Realism templates
2. fields declared by rule files
3. modType-specific additions
4. final output allowlist pruning

Main code anchors:

- default weapon template: CreateDefaultWeaponTemplate in RealismPatchGenerator.Core/StaticData.cs
- default ammo template: CreateDefaultAmmoTemplate in RealismPatchGenerator.Core/StaticData.cs
- default gear template: CreateDefaultGearTemplate in RealismPatchGenerator.Core/StaticData.cs
- default attachment template: CreateDefaultModTemplate in RealismPatchGenerator.Core/StaticData.cs
- modType-specific attributes: ModTypeSpecificAttributes in RealismPatchGenerator.Core/StaticData.cs
- final pruning: CreateAllowedFieldMap / AddRuleAllowedFields / AddRequiredAllowedFields / PruneDisallowedOutputFields in RealismPatchGenerator.Core/RealismPatchGenerator.cs

## 3. Global Principles

All categories share these common fields:

- $type: Realism patch type marker
- ItemID: item template ID, must match object key
- Name: display/debug name
- ConflictingItems: additional conflict list merged from source conflicts; currently the only generic structural field intentionally allowed to pass through

Fields that are not Realism-standard output fields:

- TemplateID
- itemTplToClone
- ItemToClone
- clone
- overrideProperties / OverrideProperties
- Prefab
- traderItems
- barterScheme
- StaticLootContainer / StaticLootContainers
- SingleFireRate
- Cartridges
- Slots
- AimSensitivity
- CalibrationDistances
- ModesCount
- IsAdjustableOptic
- sightModType

These can be used during recognition, cloning, category inference, or internal calculations, but they are not emitted to final patch output.

## 4. Weapon Standard Fields

Fixed weapon type: RealismMod.Gun, RealismMod

Major groups include:

- identification: $type, ItemID, Name, ConflictingItems
- category/operation: WeapType, OperationType, HasShoulderContact, IsManuallyOperated, WeaponAllowADS
- core stats: WeapAccuracy, BaseTorque, Ergonomics, VerticalRecoil, HorizontalRecoil, Dispersion, CameraRecoil, VisualMulti, Convergence, RecoilAngle, RecoilIntensity
- reliability/thermal: BaseMalfunctionChance, HeatFactorGun, HeatFactorByShot, CoolFactorGun, CoolFactorGunMods, AllowOverheat, DurabilityBurnRatio, BaseFixSpeed
- handling extras: CenterOfImpact, hip-fire restoration fields, ShotgunDispersion, Velocity
- ROF/reload/chamber: AutoROF, SemiROF, BurstShotsCount, reload/chamber speed fields
- visual recoil toggles: EnableBSGVisRecoil, ReduceBSGVisRecoil
- trade/weight: Weight, LoyaltyLevel, Price

## 5. Attachment Standard Fields

Fixed attachment type: RealismMod.WeaponMod, RealismMod

Common fields:

- $type, ItemID, Name
- ModType
- ConflictingItems
- Ergonomics, Weight, LoyaltyLevel, Price
- VerticalRecoil, HorizontalRecoil
- AimSpeed, Accuracy

Common extension fields (allowlist controlled):

- Dispersion
- CameraRecoil
- AimStability
- Flash
- HeatFactor
- CoolFactor
- Handling
- ReloadSpeed
- LoadUnloadModifier
- CheckTimeModifier
- ModMalfunctionChance
- DurabilityBurnModificator

modType-specific output fields include (depending on modType):

- HasShoulderContact, BlocksFolding, StockAllowADS
- AutoROF, SemiROF, Convergence, CenterOfImpact
- Velocity, Loudness, CanCycleSubs
- RecoilAngle, ModShotDispersion
- ChamberSpeed, FixSpeed, MalfunctionChance
- MeleeDamage, MeleePen

Typical boundaries:

- magazine allows LoadUnloadModifier and CheckTimeModifier, but not Cartridges
- sight only allows Realism-defined optics fields, not AimSensitivity/CalibrationDistances/ModesCount/IsAdjustableOptic/sightModType
- receiver/handguard/mount field allowance is controlled by default templates + rule allowlists + modType table; source Slots never pass into final output

## 6. Gear Standard Fields

Fixed gear type: RealismMod.Gear, RealismMod

Core fields include:

- $type, ItemID, Name
- AllowADS
- LoyaltyLevel
- TemplateType
- Price
- Weight

Protection/handling fields include:

- ArmorClass
- CanSpall
- SpallReduction
- ReloadSpeedMulti
- Comfort
- speedPenaltyPercent
- mousePenalty
- weaponErgonomicPenalty
- GasProtection
- RadProtection
- dB

## 7. Ammo Standard Fields

Fixed ammo type: RealismMod.Ammo, RealismMod

Core fields include:

- $type, ItemID, Name
- Damage, PenetrationPower
- LoyaltyLevel, BasePriceModifier
- InitialSpeed, BulletMassGram, BallisticCoeficient
- Weight
- DurabilityBurnModificator
- ammoRec, ammoAccr, ArmorDamage
- HeatFactor
- HeavyBleedingDelta, LightBleedingDelta
- MalfMisfireChance, MisfireChance, MalfFeedChance

## 8. Consumable Standard Fields

The repository keeps a consumable default template, but consumable is not a primary generation/audit focus in the current workflow.

If enabled later, typical fields include:

- $type
- Name
- TemplateType
- LoyaltyLevel
- BasePriceModifier
- ConsumableType
- Duration
- Delay
- EffectPeriod
- WaitPeriod
- Strength
- TunnelVisionStrength
- CanBeUsedInRaid

## 9. Source Fields vs Standard Output Fields

Three different concepts must be separated:

1. readable from input
2. usable for inference/computation
3. allowed in final output

Examples:

- TemplateID can be used for standard-template clone resolution, but is not emitted
- itemTplToClone can be used in WTT clone resolution, but is not emitted
- SingleFireRate may exist in source input but is not a current standard weapon output field
- Cartridges may be used internally for recognition but is not a standard attachment output field
- Slots may exist in source structures but are not Realism-standard output fields

## 10. Maintenance Requirement

Whenever standard output fields are added or removed, update all three:

1. default templates or modType field definitions
2. final output allowlist/pruning logic
3. this document and regression tests

Updating only one layer can cause field-boundary drift again.
