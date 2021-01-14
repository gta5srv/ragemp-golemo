-- phpMyAdmin SQL Dump
-- version 5.0.1
-- https://www.phpmyadmin.net/
--
-- Хост: 127.0.0.1
-- Время создания: Янв 11 2021 г., 12:05
-- Версия сервера: 10.4.11-MariaDB
-- Версия PHP: 7.2.28

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- База данных: `golemologs`
--

-- --------------------------------------------------------

--
-- Структура таблицы `adminlog`
--

CREATE TABLE `adminlog` (
  `time` datetime NOT NULL,
  `admin` text NOT NULL,
  `action` text NOT NULL,
  `player` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Структура таблицы `arrestlog`
--

CREATE TABLE `arrestlog` (
  `time` datetime NOT NULL,
  `player` text NOT NULL,
  `target` text NOT NULL,
  `reason` text NOT NULL,
  `stars` text NOT NULL,
  `pnick` text NOT NULL,
  `tnick` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Структура таблицы `banlog`
--

CREATE TABLE `banlog` (
  `time` datetime NOT NULL,
  `admin` text NOT NULL,
  `player` text NOT NULL,
  `until` datetime NOT NULL,
  `reason` text NOT NULL,
  `ishard` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Структура таблицы `connlog`
--

CREATE TABLE `connlog` (
  `uuid` text NOT NULL,
  `in` datetime NOT NULL,
  `out` datetime DEFAULT NULL,
  `sclub` text NOT NULL,
  `hwid` text NOT NULL,
  `ip` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Структура таблицы `deletelog`
--

CREATE TABLE `deletelog` (
  `time` datetime NOT NULL,
  `uuid` text NOT NULL,
  `name` text NOT NULL,
  `account` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Структура таблицы `eventslog`
--

CREATE TABLE `eventslog` (
  `AdminStarted` text NOT NULL,
  `EventName` text NOT NULL,
  `MembersLimit` text NOT NULL,
  `Started` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Структура таблицы `idlog`
--

CREATE TABLE `idlog` (
  `in` datetime NOT NULL,
  `out` datetime DEFAULT NULL,
  `uuid` text NOT NULL,
  `id` text NOT NULL,
  `name` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Структура таблицы `itemslog`
--

CREATE TABLE `itemslog` (
  `time` datetime NOT NULL,
  `from` text NOT NULL,
  `to` text NOT NULL,
  `type` text NOT NULL,
  `amount` text NOT NULL,
  `data` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Структура таблицы `moneylog`
--

CREATE TABLE `moneylog` (
  `time` datetime NOT NULL,
  `from` text NOT NULL,
  `to` text NOT NULL,
  `amount` text NOT NULL,
  `comment` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Структура таблицы `namelog`
--

CREATE TABLE `namelog` (
  `time` datetime NOT NULL,
  `uuid` text NOT NULL,
  `old` text NOT NULL,
  `new` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Структура таблицы `stocklog`
--

CREATE TABLE `stocklog` (
  `time` datetime NOT NULL,
  `frac` text NOT NULL,
  `uuid` text NOT NULL,
  `type` text NOT NULL,
  `amount` text NOT NULL,
  `in` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- --------------------------------------------------------

--
-- Структура таблицы `ticketlog`
--

CREATE TABLE `ticketlog` (
  `time` datetime NOT NULL,
  `player` text NOT NULL,
  `target` text NOT NULL,
  `sum` text NOT NULL,
  `reason` text NOT NULL,
  `pnick` text NOT NULL,
  `tnick` text NOT NULL,
  `idkey` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

--
-- Индексы сохранённых таблиц
--

--
-- Индексы таблицы `adminlog`
--
ALTER TABLE `adminlog`
  ADD PRIMARY KEY (`idkey`);

--
-- Индексы таблицы `arrestlog`
--
ALTER TABLE `arrestlog`
  ADD PRIMARY KEY (`idkey`);

--
-- Индексы таблицы `banlog`
--
ALTER TABLE `banlog`
  ADD PRIMARY KEY (`idkey`);

--
-- Индексы таблицы `connlog`
--
ALTER TABLE `connlog`
  ADD PRIMARY KEY (`idkey`);

--
-- Индексы таблицы `deletelog`
--
ALTER TABLE `deletelog`
  ADD PRIMARY KEY (`idkey`);

--
-- Индексы таблицы `eventslog`
--
ALTER TABLE `eventslog`
  ADD PRIMARY KEY (`idkey`);

--
-- Индексы таблицы `idlog`
--
ALTER TABLE `idlog`
  ADD PRIMARY KEY (`idkey`);

--
-- Индексы таблицы `itemslog`
--
ALTER TABLE `itemslog`
  ADD PRIMARY KEY (`idkey`);

--
-- Индексы таблицы `moneylog`
--
ALTER TABLE `moneylog`
  ADD PRIMARY KEY (`idkey`);

--
-- Индексы таблицы `namelog`
--
ALTER TABLE `namelog`
  ADD PRIMARY KEY (`idkey`);

--
-- Индексы таблицы `stocklog`
--
ALTER TABLE `stocklog`
  ADD PRIMARY KEY (`idkey`);

--
-- Индексы таблицы `ticketlog`
--
ALTER TABLE `ticketlog`
  ADD PRIMARY KEY (`idkey`);

--
-- AUTO_INCREMENT для сохранённых таблиц
--

--
-- AUTO_INCREMENT для таблицы `adminlog`
--
ALTER TABLE `adminlog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `arrestlog`
--
ALTER TABLE `arrestlog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `banlog`
--
ALTER TABLE `banlog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `connlog`
--
ALTER TABLE `connlog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3350;

--
-- AUTO_INCREMENT для таблицы `deletelog`
--
ALTER TABLE `deletelog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=14;

--
-- AUTO_INCREMENT для таблицы `eventslog`
--
ALTER TABLE `eventslog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=32;

--
-- AUTO_INCREMENT для таблицы `idlog`
--
ALTER TABLE `idlog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `itemslog`
--
ALTER TABLE `itemslog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `moneylog`
--
ALTER TABLE `moneylog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `namelog`
--
ALTER TABLE `namelog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `stocklog`
--
ALTER TABLE `stocklog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT для таблицы `ticketlog`
--
ALTER TABLE `ticketlog`
  MODIFY `idkey` int(11) NOT NULL AUTO_INCREMENT;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
