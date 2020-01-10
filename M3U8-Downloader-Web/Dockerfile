# 添加基础镜像
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
#容器中系统的工作空间
WORKDIR /app
#拷贝当前文件夹下的文件到容器中系统的工作空间
COPY . /app
#设置Docker容器对外暴露的端口
EXPOSE 80
EXPOSE 443

#设置时区
RUN ln -sf /usr/share/zoneinfo/Asia/Shanghai /etc/localtime
RUN echo 'Asia/Shanghai' >/etc/timezone
RUN chmod 777 /app/ffmpeg-4.2.2/ffmpeg
#容器中使用 ["dotnet","系统启动的dll"] 来运行应用程序
#使用ENTRYPOINT ["dotnet","系统启动的dll"]
#或使用 CMD ["dotnet","系统启动的dll"]
ENTRYPOINT ["dotnet", "M3U8-Downloader-Web.dll"]