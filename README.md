# BugTC - 脱离卡死
类似网游手游那种 脱离卡死 功能，当用户卡死在地图外或者无法描述的场景时使用

![image](https://github.com/user-attachments/assets/df3196a7-77b6-4a0b-81cc-31ca7dc8c78b)
![image](https://github.com/user-attachments/assets/aea420a3-9ad6-42f9-ac3d-aa5a7ddf2f85)
![image](https://github.com/user-attachments/assets/c0809333-d729-4806-80da-7620971b0250)


## 介绍
BugTC 基于 CounterStrikeSharp，允许玩家在遇到 BUG 导致无法移动时使用指令传送回复活点。
插件来自CSGO时期的BUGTC插件 https://bbs.csgocn.net/thread-59.htm

## 功能
- 提供 `css_bug`、`css_tc` 指令让玩家传送回复活点
- 每回合每个玩家只能使用一次
- 死亡玩家无法使用此功能
- 使用后会向所有玩家广播提示信息

## 安装
1. 确保已安装 CounterStrikeSharp
2. 将插件文件放置在服务器的 `plugins` 目录下
3. 重启服务器或加载插件

## 提示信息
- 成功使用：`[玩家名称] 因BUG导致无法移动，TA使用了 /tc 弹出空间。`
- 已使用过：`本回合已经使用过BUG弹出，无法再次使用！`
- 死亡状态：`你已经死亡，无法使用BUG弹出!`
