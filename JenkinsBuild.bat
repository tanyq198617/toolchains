
set UNITY_PATH=E:\Editor\2019.4.40f1c1\Editor\Unity.exe
set PROJECT_PATH=E:\GSQ\XiaWorld_Android_i18n\XiaWorld
set LOG_PATH=E:\GSQ\XiaWorld_Android_i18n\XiaWorld\Build\unity_build.log
set GIT_PATH=C:\Program Files\Git\bin\git.exe
set BRANCH_NAME=%BRANCH_NAME%

echo "执行打包操作"
cd "%PROJECT_PATH%"

IF %ERRORLEVEL% NEQ 0 (
   echo "切换工作空间失败，请查看相应Log日志"
   exit /b %ERRORLEVEL%
)


CALL "%GIT_PATH%" reset --hard
CALL "%GIT_PATH%" clean -df
CALL "%GIT_PATH%" pull
CALL "%GIT_PATH%" status

IF %ERRORLEVEL% NEQ 0 (
   echo "git操作失败，请查看相应Log日志"
   exit /b %ERRORLEVEL%
)

REM 删除指定目录
set GEN_PATH=%PROJECT_PATH%\Assets\XLua\Gen
rmdir /s /q "%GEN_PATH%"


REM 执行Unity打包命令

CALL "%UNITY_PATH%" -quit -batchmode  -nographics -silent-crashes -logFile "%LOG_PATH%" -projectPath "%PROJECT_PATH%" -executeMethod Code.Editor.GSQBuildPipeline.GSQBuildMgr.ExecuteBuild  -buildTarget "%BUILD_TARGET%" -platformName "%PLATFORM_NAME%" -appVersion "%APP_VERSION%" -msic "%MISC_BUILD_PARAMS%" -onlyBuildPlayer "%BUILD_PLAYER_ONLY%"  -scriptBackend "%SCRIPT_BACKEND%"

REM 判断命令执行情况

IF %ERRORLEVEL% NEQ 0 (
   echo "打包失败，请查看相应Log日志"
   exit /b %ERRORLEVEL%
)

echo "Android 海外版打包成功"
