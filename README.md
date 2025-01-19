# XamlViewer
XamlViewer 是winui3的一个小工具，用于查看xaml文件的效果。

采用uno.winui框架来作为跨平台的UI框架，支持win10、macOS、iOS、Android、win+、web浏览器、linux 等平台。
为了支持多平台设备，移动设备使用小屏幕模式来查看预览效果

win10上现在效果无法通过加载nuget包来扩展插件支持，因为win10采用XamlReader 的加载采用 GetReferencedAssemblies() 运行程序的依赖程序来识别xaml，无法动态扩展

非win10模式下，现在暂时无法支持Markup+Extension 的扩展模式，Markup不加扩展名称可以支持，改bug因uno.winui框架识别问题尝试的bug

## 其他
为了让xaml解析可以正常添加nuget进行加载识别，尝试使用Portable.Xaml 进行识别，分支PortableXaml进行了尝试使用，但暂时无法设置Binding。所有临时停止


