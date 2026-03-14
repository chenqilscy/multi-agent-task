#!/bin/bash
# 拆分 TaskModels.cs

mkdir -p src/Core/Models/Task

# 提取并创建独立文件
grep -A 20 "class TaskDependency" src/Core/Models/Task/TaskModels.cs > src/Core/Models/Task/TaskDependency.cs
grep -A 15 "class ResourceRequirements" src/Core/Models/Task/TaskModels.cs > src/Core/Models/Task/ResourceRequirements.cs
grep -A 40 "class DecomposedTask" src/Core/Models/Task/TaskModels.cs > src/Core/Models/Task/DecomposedTask.cs

echo "✅ TaskModels.cs 拆分完成"
