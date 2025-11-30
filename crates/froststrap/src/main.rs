/*
 *  Froststrap
 *  Copyright (c) Froststrap Team
 *
 *  This file is part of Froststrap and is distributed under the terms of the
 *  GNU Affero General Public License, version 3 or later.
 *
 *  SPDX-License-Identifier: AGPL-3.0-or-later
 *
 *  Description: Main entry point for drawing, listening, and linking together
 *               all other crates together.
 */

use {
  froststrap_config::Config,
  iced::{
    Border,
    Center,
    Element,
    Length,
    Task,
    Theme,
    theme::palette::{
      Pair,
      Primary,
    },
    widget::{
      button,
      column,
      text,
    },
  },
  logger::log,
  discordrpc::{
    DiscordIpc,
    DiscordIpcClient,
    activity::{
      self,
      Assets,
      Button,
    },
  },
  std::{
    sync::{
      Arc,
      Mutex,
    },
    time::Duration,
  },
};

fn main() -> iced::Result {
  iced::application("Froststrap", Froststrap::update, Froststrap::view)
    .theme(Froststrap::theme)
    .run_with(Froststrap::new)
}

#[derive(Clone)]
struct Froststrap {
  config: Config,
  client: Arc<Mutex<DiscordIpcClient>>,
}

#[derive(Debug, Clone)]
enum Message {
  ExitProgram,
  StartRpc,
}

impl Froststrap {
  fn update(&mut self, message: Message) -> Task<Message> {
    match message {
      Message::ExitProgram => {
        _ = self.client.lock().unwrap().close();
        iced::exit()
      }
      Message::StartRpc => {
        log::info!("Starting RPC client");
        let now = std::time::Instant::now();
        let mut instance = self.client.lock().unwrap();
        _ = instance.connect();
        let payload = activity::Activity::new()
          .state("Testing Froststrap Rust rewrite")
          .assets(Assets::new().large_text("Froststrap"))
          .buttons(vec![Button::new(
            "Download Froststrap",
            "https://github.com/RealMeddsam/Froststrap/releases/latest",
          )]);
        _ = instance.set_activity(payload);
        log::info!("RPC started in {:.3}µs", now.elapsed().as_micros());
        Task::none()
      }
    }
  }

  fn view(&self) -> Element<'_, Message> {
    column![
      text("Froststrap").size(30),
      button("Close app")
        .on_press(Message::ExitProgram)
        .style(|theme: &Theme, status| {
          use button::{
            Status,
            Style,
          };
          let Primary {
            weak: Pair { color: weak, .. },
            base: Pair { color: base, .. },
            strong: Pair { color: strong, .. },
          } = theme.extended_palette().primary;

          let mut style = Style::default().with_background(iced::Background::from(match status {
            Status::Active | Status::Disabled => weak,
            Status::Hovered => base,
            Status::Pressed => strong,
          }));

          style.border = Border {
            width: 1.0,
            color: iced::Color::from_rgba8(0, 0, 0, 0.0),
            radius: self.config.theme.roundness.into(),
          };

          style
        })
        .padding(10)
    ]
    .width(Length::Fill)
    .padding(20)
    .align_x(Center)
    .into()
  }

  fn theme(&self) -> Theme {
    self.config.theme.color_scheme.clone()
  }

  fn new() -> (Self, Task<Message>) {
    let config = match Config::load(None) {
      Ok(cfg) => {
        log::success!("Config loaded successfully");
        cfg
      }
      Err(e) => {
        log::error!("{}", e.to_string());
        Config::default()
      }
    };

    let client = Arc::new(Mutex::new(
      DiscordIpcClient::new(config.general.discord_rpc_id).unwrap(),
    ));
    (
      Self { config, client },
      Task::future(async move {
        std::thread::sleep(Duration::from_millis(10));
        Message::StartRpc
      }),
    )
  }
}
