# com.soulcore.free（元包 / 工作流说明）

- **不重复包含源码**：本包在 `package.json` 中依赖 **`com.soulcore.unity`（请用 `file:../` 或你托管的 tgz/ Git URL）**。
- **免费版与标准版**的切换、限制说明，以 **`com.soulcore.unity/Documentation~/Publish-Editions-免费与标准版.md`** 为准。
- 在团结 / Unity 资源商店，可将 **本包 + 主包** 写进描述；或直接只上架 **主包** 的免费构建（由你导出时是否带 `SOUL_EDITION_FREE` 决定）。

上架 **标准版** 时，请向买家交付 **未定义** `SOUL_EDITION_FREE` 的构建。
