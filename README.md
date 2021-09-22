# SoundCore
一个简单实用的音频播放组件，可以播放或者录制pcm和wav文件（stream、byte[]），支持Linux和Windows。在Linux上面采用alsa，在Windows采用NAudio播放音频。

## 重要
master仍然在开发中，并不能直接使用。目前支持Windows音频播放和录制，Linux音频播放（仍有问题）。

## 依赖
在Linux中播放和录制时，依赖于alsa，所以请先安装以下库：
````shell
sudo apt install libasound2-dev
sudo apt install libopenal0a libopenal-dev
sudo apt install libalut0 libalut-dev
````

## How to start

####播放  

1、Create a api.  
程序会检测当前的操作系统，并返回对应的受支持的对象。  
```csharp
ISoundCore api = SoundCoreBuilder.Create(new SoundConnectionSettings());
```

2、Play  
播放一个wav文件或者wav文件数据  
```csharp
api.PlayWav(data);  //data可以是wav路径或者wav byte数组
```
播放一个pcm文件  
```csharp
api.Play(data, true);  //当播放一个完整的pcm数据时，请指定第二个参数为true
```
播放连续的pcm数据流  
```csharp
for (int i = 0; i < data.Length; i += frameSize)
{
    api.Play(SubArray(data, i, frameSize));
}
api.Play(null, true);
```

####录制  

等我先把Linux录制写完后一起编写。  

## 编译和运行  
```shell
cd SC.Play
dotnet publish -c release -r linux-arm -o YOUR_FOLDER  //Linux arm
sudo dotnet YOUR_FOLDER/SC.Play.dll
```

## 参考和引用  
1、[alsa.net](https://github.com/ZhangGaoxing/alsa.net "alsa.net")
