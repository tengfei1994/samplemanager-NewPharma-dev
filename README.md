# samplemanager-NewPharma-dev

语言 / Language: 简体中文 | [English](README.en.md)

## 项目定位

`samplemanager-NewPharma-dev` 是 NewPharma 在 Thermo Scientific SampleManager
LIMS 上进行定制开发、方案沉淀和交付验证的主仓库。它不是某一个单点功能的临时目录，而是
NewPharma 后续多个业务主题的长期工程化工作区。

## 背景与初衷

本项目的来源是把 Pharma Solution / Login Plan 等既有能力转化为更贴合
NewPharma 业务的可维护方案：用本地代码、配置、结构文件和设计文档作为事实来源，
再通过受控部署到 SampleManager 实例中验证。

仓库保留的是 NewPharma 自研部分、设计说明、配置增量和部署脚本，不保存供应商模板源码、
客户系统完整导出或敏感连接信息。

## 当前专题：Inspection Request

当前开发重点是 Inspection Request（IR）。IR 是 NewPharma 整体改造计划中的第一个业务专题，
不是仓库的全部边界。它的目标是把请验从简单表维护对象提升为可配置、可流转、可执行的业务实体。

IR 当前目标包括：

- 通过专用 workflow login 创建 IR。
- 使用 IR 自己的 entity template 管理 IR 基础属性。
- 从选择的 Login Plan 加载 Data Assignment，并保存为 IR 自有快照。
- 从 Product Selection / Product Spec 加载产品与检验相关信息，并保存为 IR 自有快照。
- 允许 IR 层面调整请验参数，而不修改源 Login Plan。
- 支持基于 Lot 或 Job 上下文创建 IR。
- 后续执行时根据 IR 快照生成 Job、Sample、Test 等下游对象。

## 后续方向

IR 之后，本仓库可以继续承载更多 NewPharma 业务主题。新增主题应尽量保持独立的设计说明、
配置增量、表结构、界面和代码边界，避免把所有内容混在一个“大功能”里。

## 信息边界

公开 README 只描述项目目标、业务范围、目录结构和开发原则。服务器地址、SSH 配置、实例路径、
账号、部署密钥、运行日志和临时导出内容不应放在 README 中。这些内容应保留在本地环境、
私有笔记或被 `.gitignore` 排除的目录中。

## 项目目录结构

```text
docs/
  project/                         项目级架构与决策记录
  topics/
    inspection-request/             IR 专题设计与生命周期说明
  operations/                       构建、部署、验证和 DLL 说明
  private/                          本地实例信息，已被 git 忽略

samplemanager/
  structure/                        SampleManager 表结构片段
  config/                           Table-loader CSV 配置
  forms/                            Form Designer XML 定义
  sql/                              SQL 初始化和修复脚本

src/
  NewPharma.InspectionRequest/      IR task assembly 与 form/runtime 逻辑
  NewPharma.InspectionRequest.Workflow/
                                    IR workflow node assembly

scripts/                            本地打包、部署和日志工具
out/                                生成包，已被 git 忽略
tmp-*/                              临时分析目录，已被 git 忽略
```

## 工作原则

- 本地仓库是设计和代码的事实来源。
- SampleManager 实例用于验证运行效果，不作为源码主库。
- 每个业务主题都应沉淀 purpose、design、data model、UI、workflow 和 deployment notes。
- 供应商模板源码和敏感连接信息不进入公开仓库。
- 变更应尽量小步提交，确保每次提交都能说明一个清晰的业务或技术目的。
