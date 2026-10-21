# EasyExpression 架构与设计说明

本文说明当前版本的实现结构、各模块职责、关键设计取舍和扩展方式，面向需要维护或二次开发这个库的开发者。

---

## 1. 设计目标与约束

EasyExpression 的定位是：**把一段文本表达式解析成语法树，在绑定外部参数后求值**。它同时支持算术、关系、逻辑运算和函数调用。

演进过程中有三个硬约束：

1. **不破坏已有公开 API**。这个库已有一定量的 NuGet 使用者，公开类型、成员、枚举值、属性名都保持可用，新增能力通过新增成员实现。
2. **保持历史表达式写法兼容**。`&&` 与 `&`、`||` 与 `|`、`==` 与 `=`、`=>` 与 `>=`、`=<` 与 `<=` 都必须可解析；字符串字面量不能被改写。
3. **支持面要广**。目标框架是 `netstandard2.0` 和 `net8.0`，语言版本固定为 C# 7.3，避免使用新语法。

在这些约束下，当前实现采取的总体策略是：

- 对外保留 `Expression` 作为门面（facade）和语法树节点的双重身份；
- 对内拆出词法、语法、参数绑定、求值四个独立阶段；
- 用内部的类型化值模型替代原先的 `object` + 强制转换；
- 解析一次、绑定多次、求值多次，各阶段职责单一且可单独测试。

---

## 2. 总体分层

```
                    ┌─────────────────────────────────────┐
   公开 API         │ Expression / ExpressionOptions      │
                    │ FunctionRegistry / FormulaAction    │
                    │ ExpressionException / Extensions    │
                    └──────────────┬──────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────┐
   解析层           │ Lexer  →  Parser(AstParser)  → Check │
                    │ 文本         Token 列表        AST   │
                    └──────────────┬──────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────┐
   绑定层           │ ArgumentBinder                      │
                    │ Dictionary<string,object> → AST 标记 │
                    └──────────────┬──────────────────────┘
                                   │
                    ┌──────────────▼──────────────────────┐
   求值层           │ Evaluator + ExpressionValue          │
                    │ AST → 类型化值 → object              │
                    └─────────────────────────────────────┘
```

一次完整调用的数据流：

```
new Expression(text)
    → Lexer.Tokenize
    → AstParser.ParseFull
    → Expression.CopyFrom
    → Expression.Check
    → (可选) LoadArgument(dict)
    → Evaluator.Execute
    → object (double / string / DateTime / TimeSpan / null)
```

---

## 3. 目录与职责

| 文件 | 职责 |
| --- | --- |
| `Expression.cs` | 公开门面；同时作为 AST 节点类型；保存解析结果和绑定状态 |
| `ExpressionOptions.cs` | 区域性、除零策略、函数白名单 |
| `ExpressionException.cs` | 唯一的公开异常类型 |
| `FormulaAction.cs` | 所有内置函数的实现（静态 `params object[]` 方法） |
| `FormulaEnums.cs` | `ElementType`、`FunctionType`、`Operator` 等公开枚举 |
| `FunctionAttribute.cs` | `FunctionRemarkAttribute`，内置函数的元数据 |
| `OperatorAttribute.cs` | `OperatorAttribute`，运算符的名称、优先级、符号 |
| `Extensions.cs` | 历史工具方法：`DeepCopy`、`GetOperatorObj` |
| `Internal/Lexer.cs` | 手写词法分析器，输出 `Token` 列表 |
| `Internal/Parser.cs` | 递归下降语法分析器，构建 `Expression` 树 |
| `Internal/ArgumentBinder.cs` | 把外部字典按结构绑定到数据节点 |
| `Internal/Evaluator.cs` | 遍历 AST 求值 |
| `Internal/ExpressionValue.cs` | 求值期的类型化值（内部结构体） |
| `Internal/FunctionRegistry.cs` | 函数注册表；反射自动注册内置函数 |
| `Internal/OperatorInfoCache.cs` | 运算符元数据缓存 |

---

## 4. 核心数据模型：Expression

### 4.1 一个类，两种身份

`Expression` 既是用户直接使用的门面，也是语法树的节点。这样做的原因是兼容：旧版本就把树结构暴露在 `ExpressionChildren`、`Operators` 等公开属性上，重建一套内部节点类型会破坏这些成员的可用性。

为了在保留公开属性的同时让内部逻辑准确，`Expression` 上增加了若干 `internal` 状态：

| 成员 | 含义 |
| --- | --- |
| `Options` | 当前树使用的选项，解析时传给每个子节点 |
| `IsLiteral` | 该数据节点是否来自数字或字符串字面量 |
| `IsStringLiteral` | 是否带引号（区分 `abc` 与 `'abc'`） |
| `HasBoundValue` / `BoundValue` | 是否已绑定外部值，以及原始对象 |
| `IsUnbound` | 提供了参数字典但缺少该变量 |
| `ArgumentsLoaded` | 是否执行过参数绑定，供 `Execute()` 自动绑定判断 |
| `Function` | 解析时解析到的函数委托 |
| `ArgumentsLoaded` | 避免 `Execute` 重复做空绑定 |

公开属性（`ElementType`、`DataString`、`Operators`、`ExpressionChildren` 等）继续保留并且可写。内部代码对它们做了防御性检查，因为调用方理论上可以手工改树。

### 4.2 节点类型

| `ElementType` | 用途 |
| --- | --- |
| `Data` | 变量或字面量 |
| `Function` | 函数调用 |
| `Expression` | 由运算符连接起来的子表达式 |
| `Reference` | 历史保留，解析器当前不产生 |
| `Placeholder` | 历史保留，用于补齐一元运算操作数差异，当前不产生 |

解析器实际只会生成前三种。

### 4.3 树的结构约定

- 二元运算节点：`Operators` 恰好一个元素，`ExpressionChildren` 恰好两个。
- 一元运算节点（`!`、一元 `-`）：`Operators` 恰好一个元素，`ExpressionChildren` 恰好一个。
- 函数节点：`Operators` 为空，`ExpressionChildren` 是参数列表。
- 数据节点：两者都为空（列表在内部初始化为空列表）。

`Expression.Check()` 会检查二元节点的子节点数是否等于「二元运算符数 + 1」，以及空表达式/占位节点是否恰好有一个子节点。解析成功后会自动调用一次。

> 求值器本身支持一个节点内含多个运算符的旧式树，但当前解析器不会生成这种结构。

---

## 5. 解析阶段

### 5.1 词法分析（Lexer）

`Lexer.Tokenize` 是手写扫描器，输出以下 Token：`Number`、`String`、`Identifier`、`Operator`、`LeftParen`、`RightParen`、`LeftBracket`、`RightBracket`、`Comma`、`End`。

扫描规则：

- 空白直接跳过（包括换行、制表符）；
- `'` 或 `"` 进入字符串扫描，支持两种引号；
- 运算符字符单独成 Token；两个运算符字符之间允许空白；
- 其余连续的非分隔字符作为「单词」，再用 `double.TryParse` 判断是数字还是标识符。

字符串转义规则：

| 写法 | 结果 |
| --- | --- |
| `\\` | `\` |
| `\'`（单引号字符串内） | `'` |
| `\"`（双引号字符串内） | `"` |
| `\t`、`\n` 等其他组合 | 原样保留反斜杠（便于 Windows 路径） |
| 字符串末尾只有一个 `\` | 抛出 `ExpressionException`（未闭合） |

运算符兼容在**词法层**完成，而不是像旧实现那样对整段表达式做字符串替换。这解决了两类历史问题：

- 字符串字面量 `'a==b'`、`'x||y'` 被误改；
- 变量名 `a` 与 `ab` 被部分替换。

规范化表（中间允许空白）：

| 输入 | 内部 Token |
| --- | --- |
| `&&` | `&` |
| `\|\|` | `\|` |
| `==` | `=` |
| `=>` | `>=` |
| `=<` | `<=` |
| `>=`、`<=`、`!=` | 保持不变 |

### 5.2 语法分析（AstParser）

递归下降，优先级从低到高：

| 层级 | 运算符 | 解析方法 |
| --- | --- | --- |
| 1 | `&`、`\|` | `ParseLogical` |
| 3 | `=`、`!=`、`>`、`<`、`>=`、`<=` | `ParseComparison` |
| 4 | `+`、`-` | `ParseAdditive` |
| 5 | `*`、`/`、`%` | `ParseMultiplicative` |
| 6 | `!`、一元 `-` | `ParseUnary` |

所有二元运算符都是左结合。解析器采用固定优先级，与 `OperatorAttribute.Level` 中的数值保持一致，但运行时不再查询元数据。

其他细节：

- 括号返回内部节点本身，所以 `(2)` 是合法的，并且不会额外包一层节点；
- 函数语法是 `[NAME](arg1,arg2,...)`，方括号内只接受标识符；
- 函数在**解析时**查注册表，未定义函数会立刻失败；
- 函数白名单也在解析时检查，非法调用在构造阶段就抛异常；
- 最大嵌套深度为 256（`ParseExpression` 与 `ParseUnary` 都会计数），防止恶意输入造成栈溢出。

`ParseFull` 在成功后会把根节点的 `SourceExpressionString` 和（非数据节点的）`DataString` 设置为原始表达式文本，便于错误信息定位。

### 5.3 解析错误

`Parser.TryParse` 内部会捕获所有异常，把 `Status` 置为 `false`、把消息写入 `ErrorMessgage`，然后返回 `false`。`Expression.Init` 看到失败后统一抛出：

```
ExpressionException("表达式: {source} 解析错误! {detail}")
```

因此构造函数要么得到一棵可用的树，要么抛 `ExpressionException`，不会留下半成品对象。`ErrorMessage` 是拼写正确的别名，底层仍是历史属性 `ErrorMessgage`。

---

## 6. 参数绑定（ArgumentBinder）

绑定阶段的存在是为了支持「解析一次、换参数多次执行」。

### 6.1 结构绑定，不做字符串替换

`Bind` 递归遍历 AST：

- 遇到字面量：跳过，只同步 `RealityString`；
- 遇到变量：在字典中按 `DataString` 精确查找；
- 找到：记录 `HasBoundValue = true`、`BoundValue = 原始对象`，并把
  `RealityString` 设为按区域性格式化后的字符串；
- 找不到：标记 `IsUnbound = true`，`RealityString` 回退为变量名。

每次 `LoadArgument(dict)` 都会先 `Reset` 整棵树（清空 `HasBoundValue`、`BoundValue`、`IsUnbound`），因此重复绑定是幂等的，不会残留上一次的值。

### 6.2 两种绑定入口

| 调用 | 行为 |
| --- | --- |
| `LoadArgument(dict)` | 结构绑定；返回本次成功绑定的 `(参数名, 字符串值)` 列表；缺失变量标记为未绑定 |
| `LoadArgument()` | 只重置并把变量名本身当作文本值使用，保持旧版本行为 |

注意两者的差异：只有在**提供了字典但缺少某个键**时，变量才会变成 `Unbound`，参与数值运算时抛出「未提供实参」；而 `LoadArgument()` 无字典模式下，变量直接按文本处理。

### 6.3 空字符串零初始化

历史版本中，`SUM`/`AVG` 的参数如果是空字符串，会按 0 处理。绑定器保留了这一语义：

- 当父节点是 `Sum` 或 `Avg` 时，`zeroInit` 标志向下传递；
- 遇到值为 `""` 的字符串参数，替换为 `0d`；
- 该标志会继续传给更深层的子节点。

这是兼容性行为，不是通用规则，其他函数不会这样做。

---

## 7. 求值阶段

### 7.1 ExpressionValue

求值器不直接使用 `object`，而是用内部结构体 `ExpressionValue`：

| `ValueKind` | 载荷 | 来源 |
| --- | --- | --- |
| `Null` | 无 | 绑定值为 null、函数返回 null |
| `Number` | `double` | 数字、布尔、数值类型、可转数值的文本 |
| `Text` | `string` | 无法转成数字/日期的文本 |
| `Date` | `DateTime` | 可解析的日期文本、`DateTime` 参数、日期函数结果 |
| `TimeSpan` | `TimeSpan` | 日期相减、`TimeSpan` 参数 |
| `Unbound` | 变量名 | 提供字典但缺少对应键 |

文本到值的转换顺序（`FromTextValue`）：

1. `true` → 1，`false` → 0，`"1"` → 1，`"0"` → 0；
2. 以 `%` 结尾且可解析 → 数值除以 100；
3. 可解析为 `double` → 数字；
4. 可解析为 `DateTime` → 日期；
5. 否则保留为文本。

转换使用 `ExpressionOptions.Culture`。这也意味着**带引号并不强制字符串类型**：`'2024-01-01'` 会被识别为日期，`'50%'` 是 0.5，`'abc'` 才是文本。

### 7.2 节点求值

`Evaluate` 按 `ElementType` 分派：

- **Data/Reference**：优先级为
  1. `HasBoundValue` → 使用绑定值；
  2. `IsLiteral` → 转换 `DataString`；
  3. `RealityString` 与 `DataString` 不同 → 使用前者（历史机制）；
  4. 否则转换 `DataString`；若 `IsUnbound` 且结果是文本，则转成 `Unbound`。
- **Function**：解析函数定义，处理惰性函数，求值参数并调用委托。
- **Expression/Placeholder**：按运算符列表依次计算。

运算符节点求值时，`current` 从第一个子节点开始：

- 一元运算符直接作用于 `current`，不消费右侧操作数；
- 二元运算符取下一个子节点作为右操作数。

### 7.3 运算符语义

| 类别 | 规则 |
| --- | --- |
| `&`、`\|` | 两边转数值后做非零判断；**不短路**，两边都会求值 |
| `+ - * / %` | 数值运算；`Date - Date` 特判为 `TimeSpan` |
| 关系运算 | 日期比日期、时间段比时间段，其余转数值比较 |
| `=`、`!=` | 支持 null、数字、日期、时间段、文本相等比较 |
| `!` | 数值是否为 0，返回 1/0 |
| 一元 `-` | 数值取负 |

相等比较的具体规则：

- 任一方为 `Null`：只有双方都为 `Null` 才相等；
- 双方都是 `Number` / `Date` / `TimeSpan`：按对应类型比较；
- 双方都是文本或未绑定变量：按 `Ordinal` 文本比较；
- 其他组合（例如数字与文本）：不相等。

关系运算符遇到文本会尝试转数值，失败则抛异常；文本的大小比较不是字典序。

`+` **不用于字符串拼接**，拼接应使用 `[CONCAT]`。

### 7.4 函数调用

`EvaluateFunction` 的流程：

1. 按 `FunctionName` 查注册表，失败再按 `FunctionType` 查找；
2. 如果是 `IF` / `CASE`，走惰性求值分支，**不预先求值所有参数**；
3. 否则逐个求值参数；
4. 对自定义函数检查参数个数是否在 `[MinArgs, MaxArgs]` 内；
5. 调用委托，把返回值转成 `ExpressionValue`；
6. `ExpressionException` 原样抛出，其他异常包装成 `ExpressionException`。

### 7.5 IF / CASE 惰性求值

- `[IF](condition, trueValue, falseValue)`
  - 必须恰好 3 个参数；
  - 只求值条件，然后只求值命中的分支。
- `[CASE](value, p1, r1, ..., default?)`
  - 至少 2 个参数；
  - 参数按「模式, 结果」成对解释；
  - 参数个数为奇数时，最后一个是默认值；
  - 没有匹配且没有默认值时返回 `null`；
  - 只求值匹配到的结果表达式。

这两个函数在 `FormulaAction` 中有同名占位方法，直接调用会抛异常；真正的执行在求值器里。

### 7.6 除零

`Divide` 读取当前节点的 `ExpressionOptions.DivisionByZero`：

| 取值 | 结果 |
| --- | --- |
| `Infinity`（默认） | 按 IEEE 规则返回 `±Infinity` / `NaN` |
| `Zero` | 返回 0 |
| `Throw` | 抛 `ExpressionException` |

选项保存在每个节点上，因此嵌套表达式也能读到同一份配置。

---

## 8. 函数体系

### 8.1 内置函数自动注册

`FunctionRegistry` 的静态构造函数扫描 `FormulaAction` 的所有公开静态方法：

- 必须有 `FunctionRemarkAttribute`；
- 方法签名必须能绑定到 `Function` 委托（`object Method(params object[] values)`）；
- 注册名取 `FunctionRemarkAttribute.Name`；
- `FunctionType` 通过方法名与枚举名匹配得到，`Excluding` 特判为 `ContainsExcept`；
- 内置函数统一按 `0 .. int.MaxValue` 注册，因此内置函数的参数个数不在求值前校验。

注册表内部使用两个 `ConcurrentDictionary`：

- 名称 → 定义（`OrdinalIgnoreCase`）；
- `FunctionType` → 定义，用于按枚举回查。

### 8.2 自定义函数

```csharp
FunctionRegistry.Register("TWICE", values => Convert.ToDouble(values[0]) * 2, 1, 1);
FunctionRegistry.Unregister("TWICE");
```

- 名称不区分大小写，注册和查询时都会 `Trim`；
- 支持指定最少/最多参数个数；
- 同名函数默认不覆盖，`overwrite: true` 才会替换；
- 参数校验失败会抛 `ArgumentException` / `ArgumentNullException` / `ArgumentOutOfRangeException`；
- 自定义函数的 `FunctionType` 是 `Customer`。

### 8.3 白名单

`ExpressionOptions.FunctionWhitelist` 在**解析时**生效：

- `null` 或空集合表示不限制；
- 否则只有名单内函数可以出现在表达式中，匹配不区分大小写；
- 白名单同样作用于自定义函数。

放在解析期而不是求值期，是为了让非法表达式在构造阶段就失败。

### 8.4 内置函数分类

| 分类 | 函数 |
| --- | --- |
| 数学 | `SUM`、`AVG`、`ROUND` |
| 字符串 | `CONTAINS`、`EXCLUDING`、`STARTWITH`、`ENDWITH`、`EQUALS`、`DIFFERENT`、`CONCAT`、`LEN`、`SUBSTR`、`REPLACE`、`UPPER`、`LOWER`、`TRIM` |
| 逻辑 | `IF`、`CASE` |
| 日期 | `EDATE`、`EODATE`、`NOWTIME`、`TIMETOSTRING` |
| 时间段 | `DAYS`、`HOURS`、`MINUTES`、`SECONDS`、`MILLSECONDS` |
| 对象 | `ISNULL` |

---

## 9. 选项与区域性

`ExpressionOptions` 目前包含：

| 属性 | 默认值 | 作用 |
| --- | --- | --- |
| `Culture` | `CultureInfo.CurrentCulture` | 文本↔数字/日期的解析与格式化 |
| `DivisionByZero` | `Infinity` | 除零行为 |
| `FunctionWhitelist` | `null` | 函数白名单 |

选项通过 `Expression.CreateNode(options)` 传给解析器创建的每一个节点，求值器再用 `node.Options` 读取。这样做的好处是：

- 求值器保持无状态、静态；
- 不需要线程静态或全局上下文；
- 同一进程内多个表达式可以使用不同区域性。

区域性会影响：

- `Lexer` 判断一个词是否是数字；
- 绑定值字符串转数字/日期；
- 绑定值转字符串（`RealityString`）；
- 函数返回对象转字符串。

需要注意：表达式文本中的小数点写法应使用不冲突的区域性。例如 `de-DE` 下逗号是小数分隔符，但逗号同时是函数参数分隔符；`1.5` 在 `de-DE` 下可能被当成千分位。涉及小数时，建议参数通过字典以数值类型传入，或表达式统一使用 `InvariantCulture`。

---

## 10. 错误模型

公开异常只有一个：`ExpressionException`。

| 阶段 | 错误表现 |
| --- | --- |
| 构造/解析 | 直接抛 `ExpressionException`；消息含源表达式和位置 |
| `Check()` | 结构不匹配时抛 `ExpressionException` |
| `LoadArgument` | 参数缺失不会立即抛，只做标记 |
| `Execute` | 缺失参数、类型不匹配、函数失败等抛 `ExpressionException` |

求值器内部会把函数抛出的非 `ExpressionException` 包装，并在消息中带上函数名和源表达式上下文。`Expression.Execute` 外层还有一层兜底包装。

绑定阶段的缺失变量之所以延迟到求值才报错，是为了让表达式可以只使用部分参数，也便于一次性得到明确的变量名。

---

## 11. 一个完整示例

表达式：

```
a * (b + c) > d & [CONTAINS](srcText,text)
```

### 词法结果（简化）

```
Identifier(a) Operator(*) LeftParen Identifier(b) Operator(+) Identifier(c)
RightParen Operator(>) Identifier(d) Operator(&)
LeftBracket Identifier(CONTAINS) RightBracket
LeftParen Identifier(srcText) Comma Identifier(text) RightParen End
```

### 语法树

```
And(&)
├── GreaterThan(>)
│   ├── Multiply(*)
│   │   ├── Data(a)
│   │   └── Add(+)
│   │       ├── Data(b)
│   │       └── Data(c)
│   └── Data(d)
└── Function(CONTAINS, type=Contains)
    ├── Data(srcText)
    └── Data(text)
```

### 绑定

`LoadArgument` 递归命中 `a`、`b`、`c`、`d`、`srcText`、`text`，为每个节点写入绑定值和 `RealityString`；字面量不参与。返回这 6 个参数的键值对。

### 求值

自底向上：

1. `b + c` → 数值；
2. `a * (...)` → 数值；
3. 与 `d` 比较 → 1 或 0；
4. `CONTAINS` → 1 或 0；
5. `&` 合并 → 1 或 0（两边都已求值）。

因此结果为 `double` 类型的 1 或 0。

---

## 12. 关键设计取舍

### 12.1 保留可变 AST

公开属性可写、树可以被调用方修改，这是为了兼容。代价是内部需要防御空子节点、未知枚举值等情况。测试中专门覆盖了这些防御分支。

### 12.2 解析一次，绑定多次

绑定状态直接挂在树节点上，而不是每次求值重新解析字典。优点是性能和行为稳定；代价是同一个 `Expression` 实例不能并发绑定/求值，多线程场景应为每个线程准备独立实例。

### 12.3 词法层兼容而不是字符串替换

旧实现通过 `Replace` 改写整段文本。新实现把兼容规则放在 Token 生成阶段，从根本上避免字符串和变量名被误伤。

### 12.4 类型化求值

`ExpressionValue` 让「文本 1」和「数字 1」、「日期」和「日期文本」在求值期有明确区分，避免反复 `Convert` 和隐式类型错误。同时保留 `ToObject()` 作为出口，公开返回类型仍是 `double`/`string`/`DateTime`/`TimeSpan`/`null`。

### 12.5 解释执行而不是动态编译

没有使用表达式树编译或动态代码生成，原因是：

- `netstandard2.0` 和部分运行环境对动态代码支持有限；
- 表达式通常很短，解释执行足够；
- 参数以 `object` 形式动态传入，编译缓存的收益不明显。

### 12.6 全局函数注册表

`FunctionRegistry` 是静态的，注册一次全局可用。它线程安全，但也是进程级状态：测试或插件场景需要注意注册/注销的相互影响。

---

## 13. 扩展指南

### 13.1 新增内置函数

1. 在 `FunctionType` 中增加枚举值（`Customer = 100` 之前）。
2. 在 `FormulaAction` 中增加：

   ```csharp
   [FunctionRemark("MYFUNC", "[MYFUNC]", "分组", "参数说明", "返回值说明", "描述")]
   public static object MyFunc(params object[] values)
   {
       // ...
   }
   ```

   方法名与枚举名一致（不区分大小写）会自动得到对应 `FunctionType`；否则需要修改 `ResolveFunctionType`。
3. 如果函数需要惰性求值或特殊调用协议，在 `Evaluator.EvaluateFunction` 中按 `definition.Type` 拦截。
4. 在 `ExpressionTests` 中补充正常路径、边界和异常路径。

注意：自动注册要求方法是 `public static` 且签名兼容 `object Method(params object[] values)`。

### 13.2 新增运算符

1. 在 `Operator` 枚举中增加值，并标注 `[Operator(name, level, symbol)]`。
2. 在 `Lexer.IsOperatorChar` 中加入符号；双字符运算符还要改 `NormalizeOperatorPair`。
3. 在 `AstParser.ToOperator` 中映射；把运算符加入对应优先级的解析循环（或 `ParseUnary`）。
4. 在 `Evaluator.ApplyUnary` / `ApplyBinary` 中实现语义。
5. `OperatorInfoCache` 会自动从特性构建缓存。
6. 补充测试，尤其是优先级和结合性。

### 13.3 新增选项

1. 在 `ExpressionOptions` 中增加属性。
2. 在需要的位置通过 `Evaluator.GetOptions(node)` 读取；解析期选项可直接使用 `_options`。
3. 补充默认值和行为测试。

---

## 14. 测试与质量

测试项目是 SDK 风格，目标框架 `net472;net8.0`，使用 MSTest。

当前测试覆盖：

- 原有基础用例（运算、函数、日期、时间段、参数绑定）；
- 历史缺陷回归（空白表达式、单值括号、字面量改写、参数部分替换、`EODate` 跨年等）；
- 新能力（`IF`/`CASE` 惰性、字符串函数、除零策略、白名单、多区域性）；
- 公共 API 行为（`LoadArgument` 返回值、`GetAllParams`、`Check`、属性别名）；
- 词法与语法边界（双引号、转义、科学计数法、嵌套深度、长运算符链）；
- 求值器防御分支（手工篡改 AST）；
- 注册表参数校验、覆盖、注销、自定义函数返回类型与异常；
- 随机表达式 fuzz（固定种子）和畸形表达式语料，保证只抛 `ExpressionException`。

覆盖率（net8.0）：

```
行覆盖   97.2%
分支覆盖 93.1%
```

未覆盖部分主要是公开 API 无法触达的内部防御代码和无参内部工厂方法，继续提升的收益很低。

---

## 15. 已知限制

1. **内置函数不校验参数个数**。它们按 `0 .. int.MaxValue` 注册，缺参时通常在函数内部触发异常再被包装成 `ExpressionException`；自定义函数则会得到明确的参数个数错误。
2. **不使用短路逻辑**。`&` 和 `|` 两边都会求值；需要条件分支请用 `[IF]` / `[CASE]`。
3. **没有强制字符串字面量语法**。看起来像数字或日期的引号文本仍会被转换，需要文本时可通过函数结果或参数对象获得。
4. **表达式文本的小数点受区域性影响**，且逗号与参数分隔符冲突；跨区域性场景建议使用 `InvariantCulture` 或数值参数。
5. **实例不是线程安全的**。绑定状态保存在树上，并发绑定/求值需要独立实例。
6. **`FunctionRegistry` 是进程级全局状态**，注销内置函数会影响所有后续表达式。
7. **部分内置函数内部使用当前区域性**（例如 `ROUND` 的字符串数字解析、`TIMETOSTRING` 的格式化），没有完全遵循 `ExpressionOptions.Culture`。
8. **`+` 不拼接字符串**，请使用 `[CONCAT]`。