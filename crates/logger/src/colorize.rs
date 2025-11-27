/*
 *  Froststrap (logger)
 *  Copyright (c) Froststrap Team
 *
 *  This file is part of Froststrap and is distributed under the terms of the
 *  GNU Affero General Public License, version 3 or later.
 *
 *  SPDX-License-Identifier: AGPL-3.0-or-later
 *
 *  Description: Allows for colorizing of text wrapping around ANSI escapes
 *               to make coloring text done the rust idiomatic way.
 */

#[allow(dead_code)]
pub struct ColoredString {
  text: String,
  color: Option<&'static str>,
  is_bold: bool,
}

#[allow(dead_code)]
pub enum Color {
  Cyan,
  Green,
  Yellow,
  Red,
  Magenta,
  Blue,
}

#[allow(dead_code)]
impl ColoredString {
  const fn new(text: String) -> Self {
    Self {
      text,
      color: None,
      is_bold: false,
    }
  }

  pub const fn color(mut self, color: &Color) -> Self {
    self.color = Some(match color {
      Color::Cyan => "\x1b[36m",
      Color::Green => "\x1b[32m",
      Color::Yellow => "\x1b[33m",
      Color::Red => "\x1b[31m",
      Color::Magenta => "\x1b[35m",
      Color::Blue => "\x1b[34m",
    });
    self
  }

  pub const fn bold(mut self) -> Self {
    self.is_bold = true;
    self
  }
}

impl std::fmt::Display for ColoredString {
  fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
    let bold = if self.is_bold { "\x1b[1m" } else { "" };
    let color = self.color.unwrap_or("");
    let reset = "\x1b[0m";
    write!(f, "{}{}{}{}", color, bold, self.text, reset)
  }
}

#[allow(dead_code)]
pub trait ColorExt {
  fn color(self, color: Color) -> String;
  fn bold(self) -> String;
}

#[allow(dead_code)]
impl ColorExt for String {
  fn color(self, color: Color) -> String {
    let colored = ColoredString::new(self).color(&color);
    format!("{}", colored)
  }

  fn bold(self) -> String {
    let bolded = ColoredString::new(self).bold();
    format!("{}", bolded)
  }
}

impl ColorExt for &str {
  fn color(self, color: Color) -> String {
    let colored = ColoredString::new(self.to_string()).color(&color);
    format!("{}", colored)
  }

  fn bold(self) -> String {
    let bolded = ColoredString::new(self.to_string()).bold();
    format!("{}", bolded)
  }
}
