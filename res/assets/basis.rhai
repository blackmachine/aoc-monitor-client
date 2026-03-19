// fn init() {
//     static frame = 0;
// }

fn update(events) { // <--- Заметь, time больше не передается аргументом!
    // // TIME просто существует! Это глобальная переменная, которую обновил Rust.
    // let h = (TIME / 3600.0).to_int();
    // let m = ((TIME % 3600.0) / 60.0).to_int();
    // let s = (TIME % 60.0).to_int();
    
    set_buffer("main_buf");
    set_cursor(1, 1);
    set_fg(127, 255, 255, 255);
    write_text(`AOC[noisemachine]`);
    // set_cursor(4, 21);
    // write_text(`FRAME: ${frame}    `);



    // События обрабатываем как обычно
    // for e in events {
    //     if e.type == "MousePress" {
    //         // ...
    //         set_cursor(4, 21);
    //     }
    // }


    // for e in events {
    //     if e.type == "MousePress" {
    //         set_cursor(4, 21);
    //         set_fg(255, 64, 32, 255);
    //         // Используем обратные кавычки для интерполяции!
    //         write_text(`MOUSE CLICKED AT: ${e.x} X ${e.y} (${e.button.to_upper()})     `);
    //     }
        
    //     if e.type == "KeyPress" {
    //         set_cursor(4, 22);
    //         set_fg(64, 255, 64, 255);
    //         write_text(`KEY PRESSED CODE: ${e.key}   `);
    //     }
        
    //     if e.type == "FileDrop" {
    //         set_cursor(4, 23);
    //         set_fg(0, 255, 255, 255);
    //         write_text(`FILE DROPPED: ${e.path.to_upper()}`);
    //     }
    // }


}