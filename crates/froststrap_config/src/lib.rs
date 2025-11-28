/*
 *  Froststrap (config)
 *  Copyright (c) Froststrap Team
 *
 *  This file is part of Froststrap and is distributed under the terms of the
 *  GNU Affero General Public License, version 3 or later.
 *
 *  SPDX-License-Identifier: AGPL-3.0-or-later
 *
 *  Description: Main entry point for the configuration crate, including the
 *               structs for the config.
 */

const FROSTSTRAP_STAGING: bool = true;

use {
  iced::{
    Color,
    Theme,
    border::Radius,
    font,
    theme::Palette,
  },
  logger::log,
  serde::Deserialize,
  std::{
    fs,
    path::PathBuf,
    str::FromStr,
  },
};

#[derive(Debug)]
pub enum ConfigError {
  FileNotFound(PathBuf),
  ReadError(std::io::Error),
  ParseError(String),
}

impl std::fmt::Display for ConfigError {
  fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
    match self {
      Self::FileNotFound(path) => write!(f, "Config file not found at: {}", path.display()),
      Self::ReadError(e) => write!(f, "Failed to read config file: {e}"),
      Self::ParseError(e) => write!(f, "Failed to parse TOML: {e}"),
    }
  }
}

#[derive(Debug, Clone, Copy, Deserialize)]
#[serde(rename_all = "kebab-case")]
pub enum Roundness {
  Sharp,
  Normal,
  Round,
  Oval,
}

impl From<Roundness> for Radius {
  fn from(value: Roundness) -> Self {
    match value {
      Roundness::Sharp => 0.0.into(),
      Roundness::Normal => 7.0.into(),
      Roundness::Round => 10.0.into(),
      Roundness::Oval => 200.0.into(),
    }
  }
}

impl Default for Roundness {
  fn default() -> Self {
    Roundness::Normal
  }
}

#[derive(Debug, Clone, Deserialize)]
pub struct Theming {
  #[serde(default, deserialize_with = "deserialize_theme")]
  pub color_scheme: Theme,
  #[serde(default, deserialize_with = "deserialize_font")]
  pub font: font::Font,
  #[serde(default)]
  pub roundness: Roundness,
}

fn deserialize_font<'de, D>(deserializer: D) -> Result<iced::font::Font, D::Error>
where
  D: serde::Deserializer<'de>,
{
  let d = String::deserialize(deserializer)?;

  match d.trim() {
    "monospace" => Ok(font::Font::MONOSPACE),
    "normal" => Ok(font::Font::DEFAULT),
    x => Err(serde::de::Error::custom(format!(
      "Unknown font-type: {}",
      x
    ))),
  }
}

fn deserialize_theme<'de, D>(deserializer: D) -> Result<iced::Theme, D::Error>
where
  D: serde::Deserializer<'de>,
{
  let d = String::deserialize(deserializer)?;

  match d.trim() {
    "light" => Ok(iced::Theme::Light),
    "dark" => Ok(iced::Theme::Dark),
    "dracula" => Ok(iced::Theme::Dracula),
    "nord" => Ok(iced::Theme::Nord),
    "solarized-light" => Ok(iced::Theme::SolarizedLight),
    "solarized-dark" => Ok(iced::Theme::SolarizedDark),
    "gruvbox-light" => Ok(iced::Theme::GruvboxLight),
    "gruvbox-dark" => Ok(iced::Theme::GruvboxDark),
    "catpuccin-latte" => Ok(iced::Theme::CatppuccinLatte),
    "catpuccin-frappe" => Ok(iced::Theme::CatppuccinFrappe),
    "catpuccin-macchiato" => Ok(iced::Theme::CatppuccinMacchiato),
    "catpuccin-mocha" => Ok(iced::Theme::CatppuccinMocha),
    "tokyo-night" => Ok(iced::Theme::TokyoNight),
    "tokyo-night-storm" => Ok(iced::Theme::TokyoNightStorm),
    "tokyo-night-light" => Ok(iced::Theme::TokyoNightLight),
    "kanagawa-wave" => Ok(iced::Theme::KanagawaWave),
    "kanagawa-dragon" => Ok(iced::Theme::KanagawaDragon),
    "kanagawa-lotus" => Ok(iced::Theme::KanagawaLotus),
    "moonfly" => Ok(iced::Theme::Moonfly),
    "nightfly" => Ok(iced::Theme::Nightfly),
    "oxocarbon" => Ok(iced::Theme::Oxocarbon),
    "ferra" => Ok(iced::Theme::Ferra),
    "rose-pine" => Ok(Theme::custom(
      "ROSE_PINE".into(),
      Palette {
        background: Color::parse("#191724").unwrap(),
        text: Color::parse("#191724").unwrap(),
        primary: Color::parse("#ebbcba").unwrap(),
        success: Color::parse("#31748f").unwrap(),
        danger: Color::parse("#eb6f92").unwrap(),
      },
    )),
    x => Err(serde::de::Error::custom(format!("Unknown theme: {}", x))),
  }
}

impl Default for Theming {
  fn default() -> Self {
    Self {
      color_scheme: Theme::default(),
      font: font::Font::default(),
      roundness: Roundness::default(),
    }
  }
}

#[derive(Debug, Clone, Deserialize)]
pub struct General {
  #[serde(default)]
  pub discord_rpc_id: u64,
}

impl Default for General {
  fn default() -> Self {
    Self {
      discord_rpc_id: 1399535282713399418,
    }
  }
}

#[derive(Debug, Clone, Deserialize)]
pub struct Config {
  #[serde(default)]
  pub theme: Theming,
  #[serde(default)]
  pub general: General,
}

impl Default for Config {
  fn default() -> Self {
    Self {
      theme: Theming::default(),
      general: General::default(),
    }
  }
}

impl Config {
  pub fn load(config: Option<String>) -> Result<Self, ConfigError> {
    let config_path = config.map_or_else(Self::get_config_path, PathBuf::from);

    Self::load_from_path(&config_path)
  }

  pub fn load_from_path<P: AsRef<std::path::Path>>(path: P) -> Result<Self, ConfigError> {
    let config_path = path.as_ref();

    log::info!(
      "Loading config from: \x1b]8;;file://{0}\x1b\\{0}\x1b]8;;\x1b\\",
      config_path.display()
    );

    if !config_path.exists() {
      log::warning!("Config missing, creating file, but loading defaults");
      _ = fs::write(config_path, "");
      return Ok(Config::default());
    }

    let fs_str = match fs::read_to_string(config_path) {
      Ok(s) => s,
      Err(_e) => {
        log::error!("Failed to read config, using defaults");
        return Ok(Config::default());
      }
    };

    let config: Config = match toml::from_str(&fs_str) {
      Ok(cfg) => cfg,
      Err(e) => {
        log::error!("Parse failed, using defaults: {}", e);
        return Ok(Config::default());
      }
    };

    Ok(config)
  }

  pub fn get_config_path() -> PathBuf {
    if cfg!(unix) {
      std::env::home_dir()
        .unwrap()
        .join(".config")
        .join("froststrap")
        .join("config.toml")
    } else {
      PathBuf::from_str(&std::env::var("LOCALAPPDATA").unwrap())
        .unwrap()
        .join(if FROSTSTRAP_STAGING {
          "froststrap-staging"
        } else {
          "froststrap"
        })
        .join("config.toml")
    }
  }
}
