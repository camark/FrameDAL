# FrameDAL - An ORM persistance framework for C♯

## Features

 - 支持对象-关系映射，以面向对象的方式操作数据库。
 - 多种主键生成策略。支持UUID，自增长，序列等。。
 - 多数据库支持，无缝切换。在不同数据库之间切换只需更换配置文件即可，不用改动任何代码
 - 扩展性强，面向接口编程，可随时增加对其他数据库的支持
 - 支持一级Session缓存，减少连接数据库的次数，避免频繁的建立连接操作
 - 支持命名查询，把SQL写在配置文件中，实现业务逻辑代码与SQL的解耦
 - 支持事务处理。
 - 支持多线程操作。

## Usage

关于使用方法，请参考：[造轮子：一个ORM持久层框架 - Vincent's Site](http://www.liuwenjun.info/2015/11/14/FrameDAL/)