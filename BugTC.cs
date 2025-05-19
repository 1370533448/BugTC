using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BugTC
{
    public class BugTC : BasePlugin
    {
        public override string ModuleName => "BugTC";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "KodPlay";
        public override string ModuleDescription => "允许玩家在遇到BUG时使用指令传送回复活点";

        // 用于跟踪每个玩家在当前回合是否已经使用过BUG传送
        private readonly ConcurrentDictionary<ulong, bool> _usedBugTeleport = new();
        
        // 存储玩家回合开始时的位置和角度
        private readonly ConcurrentDictionary<ulong, PlayerSpawnPosition> _playerSpawnPositions = new();
        
        public override void Load(bool hotReload)
        {
            Logger.LogInformation($"[{ModuleName}]: 插件已成功加载。");
            
            // 只注册控制台命令
            AddCommand("css_bug", "使用BUG传送回复活点", CommandBug);
            AddCommand("css_tc", "使用BUG传送回复活点", CommandBug);
            
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Post);
        }

        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // 每回合开始时重置所有玩家的BUG传送使用状态
            _usedBugTeleport.Clear();
            _playerSpawnPositions.Clear();
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            // 玩家死亡时，重置其BUG传送使用状态
            if (@event.Userid != null && @event.Userid.IsValid)
            {
                _usedBugTeleport.TryRemove(@event.Userid.SteamID, out _);
            }
            return HookResult.Continue;
        }
        
        [GameEventHandler]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            // 玩家重生时，记录其位置和角度
            if (@event.Userid != null && @event.Userid.IsValid && @event.Userid.PawnIsAlive)
            {
                // 等待一小段时间，确保玩家已经完全重生
                AddTimer(0.5f, () => RecordPlayerSpawnPosition(@event.Userid));
            }
            return HookResult.Continue;
        }
        
        private void RecordPlayerSpawnPosition(CCSPlayerController player)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                return;
                
            try
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn != null && pawn.IsValid)
                {
                    // 获取玩家当前位置和角度
                    Vector position = new Vector(0, 0, 0);
                    QAngle angle = new QAngle(0, 0, 0);
                    
                    // 尝试获取玩家位置和角度
                    try {
                        position = pawn.AbsOrigin;
                        angle = pawn.AbsRotation;
                    } catch {
                        // 如果出错，使用默认值
                    }
                    
                    // 存储玩家的重生位置
                    _playerSpawnPositions[player.SteamID] = new PlayerSpawnPosition
                    {
                        Position = position,
                        Angle = angle
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recording player spawn position: {ex.Message}");
            }
        }

        // 移除聊天命令监听器

        private void CommandBug(CCSPlayerController? player, CommandInfo command)
        {
            TeleportPlayer(player);
        }

        private void TeleportPlayer(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            {
                return;
            }

            // 检查玩家是否已经死亡
            if (!player.PawnIsAlive)
            {
                player.PrintToChat($" {ChatColors.Red}你已经死亡，无法使用BUG弹出!");
                return;
            }

            // 检查玩家在当前回合是否已经使用过BUG传送
            if (_usedBugTeleport.TryGetValue(player.SteamID, out bool used) && used)
            {
                player.PrintToChat($" {ChatColors.Red}本回合已经使用过BUG弹出，无法再次使用！");
                return;
            }
            
            // 先标记玩家已经使用过BUG传送，防止重复调用
            _usedBugTeleport[player.SteamID] = true;

            // 尝试使用玩家回合开始时的位置
            Vector teleportPosition = new Vector(0, 0, 0);
            QAngle teleportAngle = new QAngle(0, 0, 0);
            bool hasSpawnPosition = false;
            
            // 检查是否有存储的重生位置
            if (_playerSpawnPositions.TryGetValue(player.SteamID, out PlayerSpawnPosition spawnPos))
            {
                teleportPosition = spawnPos.Position;
                teleportAngle = spawnPos.Angle;
                hasSpawnPosition = true;
            }
            
            // 如果没有存储的重生位置，则使用地图的出生点
            if (!hasSpawnPosition)
            {
                // 根据玩家的队伍获取适当的复活点
                string spawnEntityName;
                if (player.Team == CsTeam.Terrorist)
                    spawnEntityName = "info_player_terrorist";
                else
                    spawnEntityName = "info_player_counterterrorist";
                
                var spawnPoints = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(spawnEntityName);
                int count = 0;
                
                // 安全地获取列表长度
                try {
                    count = spawnPoints.ToList().Count;
                } catch {
                    // 如果出错，尝试其他方式
                    count = spawnPoints.Count();
                }
                
                if (spawnPoints == null || count == 0)
                {
                    player.PrintToChat($" {ChatColors.Red}无法找到复活点！");
                    return;
                }
                
                Random random = new Random();
                int index = random.Next(count);
                var spawnPoint = spawnPoints.ElementAt(index);
                
                // 尝试获取实体的位置和角度
                try
                {
                    try {
                        teleportPosition = spawnPoint.AbsOrigin;
                        teleportAngle = spawnPoint.AbsRotation;
                    } catch {
                        // 如果出错，使用默认值
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting spawn position: {ex.Message}");
                }
            }
            
            player.PlayerPawn.Value?.Teleport(teleportPosition, teleportAngle, new Vector(0, 0, 0));
            
            string teamColor = GetTeamColor(player.Team);
            Server.PrintToChatAll($" {teamColor}{player.PlayerName} {ChatColors.Default}因BUG导致无法移动，TA使用了 {ChatColors.Green}.tc {ChatColors.Default}弹出空间。");
        }

        private string GetTeamColor(CsTeam team)
        {
            if (team == CsTeam.Terrorist)
                return $"{ChatColors.Red}";    // T队
            else if (team == CsTeam.CounterTerrorist)
                return $"{ChatColors.Blue}";   // CT队
            else
                return $"{ChatColors.Default}"; // 其他队伍
        }
    }
}

// 用于存储玩家重生位置的类
public class PlayerSpawnPosition
{
    public Vector Position { get; set; } = new Vector(0, 0, 0);
    public QAngle Angle { get; set; } = new QAngle(0, 0, 0);
}
