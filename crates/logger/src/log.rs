use crate::colorize::{
  Color,
  ColorExt,
};
use std::fmt;

#[allow(dead_code)]
pub enum LogLevel {
  Info,
  Success,
  Warning,
  Error,
  Bot,
  Debug,
}

impl LogLevel {
  const fn as_str(&self) -> &'static str {
    match self {
      Self::Info => "inf",
      Self::Success => "suc",
      Self::Warning => "wrn",
      Self::Error => "err",
      Self::Bot => "bot",
      Self::Debug => "dbg",
    }
  }

  const fn get_color(&self) -> Color {
    match self {
      Self::Info => Color::Cyan,
      Self::Success => Color::Green,
      Self::Warning => Color::Yellow,
      Self::Error => Color::Red,
      Self::Bot => Color::Magenta,
      Self::Debug => Color::Blue,
    }
  }
}

pub fn log_internal(level: LogLevel, args: fmt::Arguments<'_>) {
  let (stream, color) = match level {
    LogLevel::Error => ("STDERR", level.get_color()),
    _ => ("STDOUT", level.get_color()),
  };

  println!(
    "{} {}",
    format!("[{}/{}]:", stream, level.as_str())
      .color(color)
      .bold(),
    args
  );
}

#[macro_export]
macro_rules! info {
    ($($arg:tt)*) => {
        crate::log::log_internal(crate::log::LogLevel::Info, format_args!($($arg)*));
    };
}
#[allow(unused_imports)]
pub use info;

#[macro_export]
macro_rules! success {
    ($($arg:tt)*) => {
        crate::log::log_internal(crate::log::LogLevel::Success, format_args!($($arg)*));
    };
}
#[allow(unused_imports)]
pub use success;

#[macro_export]
macro_rules! warning {
    ($($arg:tt)*) => {
        crate::log::log_internal(crate::log::LogLevel::Warning, format_args!($($arg)*));
    };
}
#[allow(unused_imports)]
pub use warning;

#[macro_export]
macro_rules! error {
    ($($arg:tt)*) => {
        crate::log::log_internal(crate::log::LogLevel::Error, format_args!($($arg)*));
    };
}
#[allow(unused_imports)]
pub use error;

#[macro_export]
macro_rules! bot {
    ($($arg:tt)*) => {
        crate::log::log_internal(crate::log::LogLevel::Bot, format_args!($($arg)*));
    };
}
#[allow(unused_imports)]
pub use bot;

#[macro_export]
macro_rules! debug {
    ($($arg:tt)*) => {
        crate::log::log_internal(crate::log::LogLevel::Debug, format_args!($($arg)*));
    };
}
#[allow(unused_imports)]
pub use debug;
