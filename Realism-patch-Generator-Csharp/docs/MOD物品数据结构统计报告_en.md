# input/user_templates Structure Statistics

Scope: all JSON files under input/user_templates, total 172 files.

Two statistical lenses are used:

- exact structural signatures: exact key-set grouping per item object, 51 signatures
- broad structural families: grouped by core marker fields, excluding author-made realism patch files and empty samples, 4 families

This report focuses on the 4 effective third-party input structure families that are easier for manual review.

## 1. WTT_templates

Recognition marker: item object contains itemTplToClone.

Current state: old monolithic WTT_templates recognition/output logic has been removed. The current code supports these 8 subclasses:

- WttArmory_templates
- Epic_templates
- ConsortiumOfThings_templates
- Requisitions_templates
- EcoAttachment_templates
- Artem_templates
- WttStandalone_templates
- SptBattlepass_templates

Grouping is still source-mod oriented, but only the 8 subclasses above are currently in the formal recognition chain.

Current file-name based subgroup rules:

- WttArmory_templates: filename contains WTT - Armory_
- ConsortiumOfThings_templates: filename contains ConsortiumOfThings_
- Requisitions_templates: filename contains Echoes.of.Tarkov.-.Requisitions_
- EcoAttachment_templates: filename contains Eco-Attachment Emporium_
- Epic_templates: filename contains EpicRangeTime-
- Artem_templates: filename contains Artem_
- WttStandalone_templates: filename contains AK50, AKResonant, 50 BMG, or .50BMG (for AK50/AKResonant/.50BMG Remaster style sources)
- SptBattlepass_templates: filename contains SPT Battlepass

This classification is used to incrementally maintain dedicated subclass logic. All currently supported subclasses drive their own parentId/template hint/output path.

File count: 149 (including 4 WttStandalone_templates files and 1 SptBattlepass_templates file).

Representative files include:

- input/user_templates/[2]新物品-竞技场赛季奖励-SPT Battlepass.json
- input/user_templates/[3].50BMG重制-Epics 50 BMG Remaster-Expansion.json
- input/user_templates/[3]新武器-AK50_items.json
- input/user_templates/[3]新武器-AK50_Weapon.json
- input/user_templates/[3]新武器-AKResonant.json
- input/user_templates/[3]新武器-WTT武器库-WTT - Armory_Ammo.json
- input/user_templates/[3]新配件-ConsortiumOfThings_Attachments.json
- input/user_templates/[3]新配件-Echoes.of.Tarkov.-.Requisitions_Attachments.json
- input/user_templates/[3]新配件-Eco-Attachment Emporium_AR_15_Stocks.json
- input/user_templates/[3]新配件-EpicRangeTime-Weapons_Scopes.json
- input/user_templates/[4]新商人-Artem_Backpacks.json

## 2. RaidOverhaul_templates

Recognition marker: item object contains ItemToClone.

Current state: fully supported for recognition and output.

File count: 16

Representative files:

- input/user_templates/[5]战局大修-RaidOverhaul_ConstItems/DeadSkul.json
- input/user_templates/[5]战局大修-RaidOverhaul_Gear/Carrion.json
- input/user_templates/[5]战局大修-RaidOverhaul_Weapons/Aug.json

## 3. Mixed_templates

Recognition marker: same file contains clone + item structure and also item-only/items-only approximate structures.

Current state: fully supported for recognition and output.

File count: 3

- input/user_templates/[2]新装备、衣服-TacticalGearComponent.json
- input/user_templates/[3]新武器-SIG_MCX_VIRTUS_items.json
- input/user_templates/[3]新武器-国产武器-1SD-QBZ191_items.json

## 4. Moxo_Template

Recognition marker: item object contains clone plus item or items.

Current state: fully supported for recognition and output.

File count: 2

- input/user_templates/[3]新配件-BlackCore.json
- input/user_templates/[3]新配件-MagTape.json

## Conclusion

- exact key-set statistics: 51 structure signatures
- broad, human-readable structure families (excluding author-made realism patches and empty samples): 4
- primary effective third-party input families are:
  - itemTplToClone
  - ItemToClone
  - Mixed (clone+item, item/items)
  - clone+item

Note: the exact complete file list in the Chinese source report remains authoritative; this English copy preserves structure and key conclusions for cross-language usage.
