// #![windows_subsystem = "windows"]

use serde::Deserialize;
use std::error::Error;
use std::sync::mpsc;
use std::thread;
use std::time::Duration;
use std::time::Instant;

use raylib::prelude::*;

use voidgrid::VoidGrid;
use voidgrid::hierarchy::Hierarchy;
use voidgrid::text_ops::TextOps;


use voidgrid::pack_loader::PackLoader;



#[derive(Deserialize, Debug)]
struct ServerStatus {
    is_online: bool,
    is_restarting: bool,
    player_count: usize,
    players: Vec<String>,
}



fn main() -> Result<(), Box<dyn Error>> {

    let (mut rl, thread) = raylib::init()
        .size(800, 600)
        .title("Noisemachine AOC Server monitor")
        // .undecorated()
        // .resizable()
        .build();

    rl.set_target_fps(60);

    let (tx, rx) = mpsc::channel::<Result<ServerStatus, reqwest::Error>>();
    thread::spawn(move||{
        let url = "http://91.214.241.217:9999/status";

        let client = reqwest::blocking::Client::builder()
        .timeout(Duration::from_secs(3))
        .build()
        .expect("Не удалось инициализировать HTTP-клиент");

        loop {
            println!("Запрашиваем статус у монитора...");

            let response_result = client.get(url).send();

        match response_result {
            Ok(response) => {
                // Сервер ответил! Пробуем распарсить JSON
                match response.json::<ServerStatus>() {
                    Ok(status) => {
                        let _ = tx.send(Ok(status));
                    }
                    Err(parse_error) => {
                        // Ответ пришел, но это не наш JSON (например, заглушка провайдера)
                        let _ = tx.send(Err(parse_error));
                    }
                }
            }
            Err(network_error) => {
                // Ошибка сети (интернет отвалился, сервер недоступен, ИЛИ сработал наш таймаут!)
                let _ = tx.send(Err(network_error));
            }
        }

            thread::sleep(Duration::from_secs(2));
        }
    });




    let mut buf_w: u32 = 32;
    let mut buf_h: u32 = 32;

    let mut vg = VoidGrid::new();

    // let zip_file = std::fs::File::open("crt.vpk")
    //     .expect("Не удалось найти файл crtdemo.vpk");

    // let mut provider = voidgrid_resource_packs::ZipProvider::new(zip_file)
    //     .expect("Не удалось прочитать структуру ZIP-архива");

    let mut provider = voidgrid_resource_packs::DirProvider::new("res");
    vg.init(&mut provider, &mut rl, &thread);
    let mut hierarchy = Hierarchy::new();
    let pack = PackLoader::load_pack(
        &mut vg, 
        &mut hierarchy, 
        &mut provider, 
        "manifest.json", 
        &mut rl, 
        &thread
    ).expect("Failed to load scene from manifest");

    vg.terminal.register_buffers(pack.buffers.clone());

    let main_buf = pack.buffers["main_buf"];
    let back_buf = pack.buffers["back_buf"];

    let main_glyphset = vg.grids.get(main_buf).unwrap().glyphset();
    let (tile_w, tile_h) = vg.grids.assets.glyphset_size(main_glyphset).unwrap();

    let window_w = buf_w * tile_w;
    let window_h = buf_h * tile_h;
    rl.set_window_size(window_w as i32, window_h as i32);



    // --- Rhai Initialization ---
    let mut script_engine = voidgrid::scripting::ScriptEngine::new();
    
    // Авто-загрузка всех скриптов из манифеста
    for (name, code) in &pack.scripts {
        if let Err(e) = script_engine.load_script(name, code) {
            eprintln!("Failed to load pack script '{}': {}", name, e);
        }
    }

    // ВАЖНО: Вызываем run_init() только ПОСЛЕ загрузки всех скриптов из пака!
    script_engine.run_init();
    // ---------------------------
    // let mut is_resized = false;
    let mut msg_prev = String::new();

    let mut start_time: Instant;
    start_time = Instant::now();
    
    while !rl.window_should_close() {
        let mut isOk = false;

        
        let mut msg= String::new();
        let mut col = Color::new(255, 255, 255, 255);





        for recieved in rx.try_iter() {
            
            vg.grids.clear_buffer(main_buf);
            match recieved {
                Ok(status) => {

                    if status.is_online{

                        if status.is_restarting {
                        vg.grids
                        .print(main_buf)
                        .at(1, 0)
                        .fg(Color{ r: 255, g: 127, b: 0, a: 255 })
                        .write(("      \nREBOOT", "inverted"));
                        } else {


                        vg.grids
                            .print(main_buf)
                            .at(1, 0)
                            .fg(Color{ r: 0, g: 255, b: 127, a: 255 })
                            .write(("      \nONLINE", "inverted"));

                        if status.player_count > 0 {
                            vg.grids.write_string(
                                main_buf,
                                1,
                                3,
                                status.players.join(", ").to_uppercase().as_str(),
                                Color{ r: 0, g: 255, b: 127, a: 192 },
                                Color::BLANK,
                            );
                        
                        }}
                } else {
                    
                        vg.grids
                        .print(main_buf)
                        .at(1, 0)
                        .fg(Color{ r: 255, g: 127, b: 0, a: 255 })
                        .write(("       \nOFFLINE", "inverted"));
                    
                }

                    println!("Players [{}]: {}", status.player_count, status.players.join(", "));
                    

                }


                Err(e) if e.is_timeout() => {
                    vg.grids
                        .print(main_buf)
                        .at(1, 0)
                        .fg(Color{ r: 255, g: 64, b: 0, a: 255 })
                        .writeln(("     \nERROR", "inverted"))
                        .write("\nREMOTE DOWN");

                
                }
                
                Err(e) if e.is_connect() => {
                    vg.grids
                        .print(main_buf)
                        .at(1, 0)
                        .fg(Color{ r: 255, g: 64, b: 0, a: 255 })
                        .writeln(("     \nERROR", "inverted"))
                        .write("\nNO NETWORK");
                }

                Err(e) if e.is_decode() => {
                    vg.grids
                        .print(main_buf)
                        .at(1, 0)
                        .fg(Color{ r: 255, g: 64, b: 0, a: 255 })
                        .writeln(("     \nERROR", "inverted"))
                        .write("\nBAD RESPONSE");
                }
                Err(_) => {
                    vg.grids
                        .print(main_buf)
                        .at(1, 0)
                        .fg(Color{ r: 255, g: 64, b: 0, a: 255 })
                        .writeln(("     \nERROR", "inverted"))
                        .write("\n[UNKNOWN]");
                }
            }
                
        }


        let current_time = start_time.elapsed().as_secs_f32();

        for action in script_engine.take_actions() {
            vg.terminal.apply_action(&mut vg.grids, action);
        }   
            
        script_engine.sync_state(&vg.grids, &pack.buffers);
        script_engine.run_update(current_time, &vg.events.frame_events);


        let render_list = hierarchy.collect_render_list(|b| {
            if let Some(buf) = vg.grids.get(b) {
                if let Some((tw, th)) = vg.grids.assets.glyphset_size(buf.glyphset()) {
                    return (buf.w, buf.h, tw, th);
                }
            }
            (0, 0, 1, 1)
        });

        



        vg.render_offscreen(&mut rl, &thread, &render_list);
        {
            let mut d = rl.begin_drawing(&thread);
            d.clear_background(Color::new(16,16, 16, 255));
            vg.draw(&mut d, &render_list); // draw рисует дерево + применяет шейдеры к буферам (через фасад)
            }
        
    }

    Ok(())
}