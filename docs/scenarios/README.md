# CKY.MAF Demo场景用例文档

本目录包含CKY.MAF框架两个Demo项目的完整会话场景用例文档。

## 📁 目录结构

```
scenarios/
├── README.md                        # 本文件
├── SmartHome/                       # 智能家居场景
│   ├── README.md
│   ├── 01-早晨唤醒/
│   ├── 02-离家准备/
│   └── ...
├── CustomerService/                 # 智能客服场景
│   ├── README.md
│   ├── 01-初次咨询/
│   └── ...
├── by-agent/                        # 按Agent索引
├── by-complexity/                   # 按复杂度索引
├── by-test-priority/                # 按测试优先级索引
├── by-demo-value/                   # 按演示价值索引
├── by-doc-importance/               # 按文档重要性索引
├── TOTAL-INDEX.md                   # 全部74个用例总索引
└── templates/                       # 文档模板
    ├── case-template.md
    └── metadata-template.yaml
```

## 🎯 快速导航

### 按场景浏览
- [SmartHome智能家居](./SmartHome/README.md) - 36个用例，8个用户旅程
- [CustomerService智能客服](./CustomerService/README.md) - 38个用例，8个用户旅程

### 按维度索引
- [按Agent查找](./by-agent/README.md) - 开发团队使用
- [按复杂度查找](./by-complexity/README.md) - 学习路径
- [按测试优先级查找](./by-test-priority/README.md) - 测试团队使用
- [按演示价值查找](./by-demo-value/README.md) - 产品/演示团队使用
- [按文档重要性查找](./by-doc-importance/README.md) - 文档团队使用

### 总索引
- [全部74个用例总索引](./TOTAL-INDEX.md) - 一页查看所有用例

## 📊 用例统计

- **总计**: 74个用例
- **SmartHome**: 36个用例
- **CustomerService**: 38个用例
- **P0优先级**: 24个用例（核心功能）
- **高演示价值**: 52个用例（4星以上）

## 🚀 快速开始

### 如果你是开发人员
推荐按以下顺序学习：
1. 从 [L1-单Agent](./by-complexity/README.md#l1) 开始
2. 逐步学习 [L2-多轮对话](./by-complexity/README.md#l2)
3. 掌握 [L3-多Agent协作](./by-complexity/README.md#l3)
4. 研究 [L4-复杂编排](./by-complexity/README.md#l4)

### 如果你是测试人员
优先查看 [P0-必须测试](./by-test-priority/README.md#p0) 用例。

### 如果你是产品/演示人员
推荐查看 [5星-核心演示](./by-demo-value/README.md#5星) 用例。

## 📚 相关文档

- [设计文档](../plans/2026-03-15-demo-scenario-use-cases-design.md)
- [架构总览](../specs/01-architecture-overview.md)
- [实施指南](../specs/09-implementation-guide.md)

---

**最后更新**: 2026-03-15
**维护者**: CKY.MAF团队
