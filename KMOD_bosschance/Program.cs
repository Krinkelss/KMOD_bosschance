using fastJSON5;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using System.Reflection;

namespace KMOD_bosschance;

public record ModMetadata : AbstractModMetadata
{
	public override string? ModGuid { get; init; } = "4f8f9307-599f-4446-b4df-fddfa34d3743";
	public override string? Name { get; init; } = "KMOD_bosschance";
	public override string? Author { get; init; } = "Krinkels";
	public override List<string>? Contributors { get; set; } = new() { "", "" };
	public override SemanticVersioning.Version Version { get; } = new( "1.0.0" );
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
	ModHelper modHelper
) : IOnLoad
{
	//_props - Properties
	public Task OnLoad()
	{
		var locations = databaseService.GetLocations();

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

		return Task.CompletedTask;
	}

	public class BossData
	{
		public int BossChance { get; set; }
		public string? BossZones { get; set; }
	}
}