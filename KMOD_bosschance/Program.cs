using fastJSON5;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using System.Formats.Tar;
using System.Reflection;
using static KMOD_bosschance.KMOD_bosschance;

namespace KMOD_bosschance;

public record ModMetadata : AbstractModMetadata
{
	public override string? ModGuid { get; init; } = "4f8f9307-599f-4446-b4df-fddfa34d3743";
	public override string? Name { get; init; } = "KMOD_bosschance";
	public override string? Author { get; init; } = "Krinkels";
	public override List<string>? Contributors { get; set; } = new() { "", "" };
	public override SemanticVersioning.Version Version { get; } = new( "1.1.0" );
	public override SemanticVersioning.Version SptVersion { get; } = new( "4.0.0" );
	public override List<string>? LoadBefore { get; set; }
	public override List<string>? LoadAfter { get; set; }
	public override List<string>? Incompatibilities { get; set; }
	public override Dictionary<string, SemanticVersioning.Version>? ModDependencies { get; set; }
	public override string? Url { get; set; } = "";
	public override bool? IsBundleMod { get; set; } = false;
	public override string? License { get; init; } = "MIT";
}

[Injectable( TypePriority = OnLoadOrder.PostDBModLoader + 1 )]
public class KMOD_bosschance(
	ISptLogger<KMOD_bosschance> logger,
	DatabaseService databaseService,
	ConfigServer _configServer,
	ModHelper modHelper
) : IOnLoad
{
	//_props - Properties
	public Task OnLoad()
	{
		var locations = databaseService.GetLocations();

		LocationConfig locs = _configServer.GetConfig<LocationConfig>();

		// Загружаем наши настройки
		var pathToMod = modHelper.GetAbsolutePathToModFolder( Assembly.GetExecutingAssembly() );

		Dictionary<string, Dictionary<string, BossData>> Config;
		try
		{
			Config = JSON5.ToObject<Dictionary<string, Dictionary<string, BossData>>>( modHelper.GetRawFileData( pathToMod, "Config.json5" ) );
		}
		catch( Exception e )
		{
			logger.Error( $"[KMOD_bosschance] Ошибка при загрузке Config.json5: {e.Message}" );
			return Task.CompletedTask;
		}

		foreach( var mapEntry in Config )
		{
			string mapName = mapEntry.Key;

			// Используем рефлексию для получения свойства
			PropertyInfo? property = locations.GetType().GetProperty( mapName );

			if( property != null )
			{
				var locationInstance = property.GetValue( locations );
				var specificLocation = locationInstance as SPTarkov.Server.Core.Models.Eft.Common.Location;

				var bosses = mapEntry.Value;
				foreach( var bossEntry in bosses )
				{
					string bossName = bossEntry.Key;
					BossData bossData = bossEntry.Value;

					var BossUpdate = specificLocation?.Base.BossLocationSpawn
						.FirstOrDefault( boss => boss.BossEscortType == bossName );
					if( BossUpdate != null )					
					{
						BossUpdate.BossChance = bossData.BossChance;
						BossUpdate.BossZone = bossData.BossZones;
					}
					else
					{
						logger.LogWithColor( $"Не могу найти {bossName}", LogTextColor.Cyan );
						continue;
					}
					logger.LogWithColor( $"Босс \"{BossUpdate.BossName}\" на карте \"{mapEntry.Key}\" обновлён", LogTextColor.Cyan );
				}
			}
			else
			{
				logger.Error( $"Карта '{mapName}' Не найдена в списке локаций" );
			}
		}

		//locs.LooseLootMultiplier.

		LOOT Loot = null;
		try
		{
			Loot = JSON5.ToObject<LOOT>( modHelper.GetRawFileData( pathToMod, "Loot.json5" ) );
		}
		catch( Exception e )
		{
			logger.Error( $"[KMOD] Ошибка при загрузке Loot.json5: {e.Message}" );
			return Task.CompletedTask;
		}

		if( Loot.looseLoot.Enable == true )
		{			
			/*foreach( var location in locs.LooseLootMultiplier )
			{
				//mapLootMultipliers[ location.Key ] = randomUtil.GetPercentOfValue( mapLootMultipliers[ location.Key ], loosePercent ?? 1 );
				logger.LogWithColor( $"Лока {location.Key} = {locs.LooseLootMultiplier[ location.Key ]}", LogTextColor.Cyan );
			}*/
		
			locs.LooseLootMultiplier[ "bigmap" ] = Loot.looseLoot.bigmap;
			locs.LooseLootMultiplier[ "factory4_day" ] = Loot.looseLoot.factory4_day;
			locs.LooseLootMultiplier[ "factory4_night" ] = Loot.looseLoot.factory4_night;
			locs.LooseLootMultiplier[ "interchange" ] = Loot.looseLoot.interchange;
			locs.LooseLootMultiplier[ "laboratory" ] = Loot.looseLoot.laboratory;
			locs.LooseLootMultiplier[ "rezervbase" ] = Loot.looseLoot.rezervbase;
			locs.LooseLootMultiplier[ "shoreline" ] = Loot.looseLoot.shoreline;
			locs.LooseLootMultiplier[ "woods" ] = Loot.looseLoot.woods;
			locs.LooseLootMultiplier[ "lighthouse" ] = Loot.looseLoot.lighthouse;
			locs.LooseLootMultiplier[ "tarkovstreets" ] = Loot.looseLoot.tarkovstreets;
			locs.LooseLootMultiplier[ "sandbox" ] = Loot.looseLoot.sandbox;
			locs.LooseLootMultiplier[ "sandbox_high" ] = Loot.looseLoot.sandbox_high;
			locs.LooseLootMultiplier[ "labyrinth" ] = Loot.looseLoot.labyrinth;
		}

		if( Loot.staticLoot.Enable == true )
		{
			/*foreach( var location in locs.LooseLootMultiplier )
			{
				//mapLootMultipliers[ location.Key ] = randomUtil.GetPercentOfValue( mapLootMultipliers[ location.Key ], loosePercent ?? 1 );
				logger.LogWithColor( $"Лока {location.Key} = {locs.LooseLootMultiplier[ location.Key ]}", LogTextColor.Cyan );
			}*/

			locs.StaticLootMultiplier[ "bigmap" ] = Loot.staticLoot.bigmap;
			locs.StaticLootMultiplier[ "factory4_day" ] = Loot.staticLoot.factory4_day;
			locs.StaticLootMultiplier[ "factory4_night" ] = Loot.staticLoot.factory4_night;
			locs.StaticLootMultiplier[ "interchange" ] = Loot.staticLoot.interchange;
			locs.StaticLootMultiplier[ "laboratory" ] = Loot.staticLoot.laboratory;
			locs.StaticLootMultiplier[ "rezervbase" ] = Loot.staticLoot.rezervbase;
			locs.StaticLootMultiplier[ "shoreline" ] = Loot.staticLoot.shoreline;
			locs.StaticLootMultiplier[ "woods" ] = Loot.staticLoot.woods;
			locs.StaticLootMultiplier[ "lighthouse" ] = Loot.staticLoot.lighthouse;
			locs.StaticLootMultiplier[ "tarkovstreets" ] = Loot.staticLoot.tarkovstreets;
			locs.StaticLootMultiplier[ "sandbox" ] = Loot.staticLoot.sandbox;
			locs.StaticLootMultiplier[ "sandbox_high" ] = Loot.staticLoot.sandbox_high;
			locs.StaticLootMultiplier[ "labyrinth" ] = Loot.staticLoot.labyrinth;
		}

		return Task.CompletedTask;
	}

	public class BossData
	{
		public int BossChance { get; set; }
		public string? BossZones { get; set; }
	}

//*************************
	public class LooseLoot
	{
		public bool Enable { get; set; }

		public double bigmap { get; set; }
		public double factory4_day { get; set; }
		public double factory4_night { get; set; }
		public double interchange { get; set; }
		public double laboratory { get; set; }
		public double rezervbase { get; set; }
		public double shoreline { get; set; }
		public double woods { get; set; }
		public double lighthouse { get; set; }
		public double tarkovstreets { get; set; }
		public double sandbox { get; set; }
		public double sandbox_high { get; set; }
		public double labyrinth { get; set; }
	}

	public class StaticLoot
	{
		public bool Enable { get; set; }

		public double bigmap { get; set; }
		public double factory4_day { get; set; }
		public double factory4_night { get; set; }
		public double interchange { get; set; }
		public double laboratory { get; set; }
		public double rezervbase { get; set; }
		public double shoreline { get; set; }
		public double woods { get; set; }
		public double lighthouse { get; set; }
		public double tarkovstreets { get; set; }
		public double sandbox { get; set; }
		public double sandbox_high { get; set; }
		public double labyrinth { get; set; }
	}

	public class LOOT
	{
		public LooseLoot looseLoot { get; set; }
		public StaticLoot staticLoot { get; set; }
	}
}