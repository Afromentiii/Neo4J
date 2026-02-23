import os
import requests
import re

movies_data = [
    ("Iron Man", "https://image.tmdb.org/t/p/original/78lPtwv72eTNqFW9COBYI0dWDJa.jpg"),
    ("The Incredible Hulk", "https://image.tmdb.org/t/p/original/gKzYx79y0AQTL4UAk1cBQJ3nvrm.jpg"),
    ("Iron Man 2", "https://image.tmdb.org/t/p/original/6WBeq4fCfn7AN0o21W9qNcRF2l9.jpg"),
    ("Thor", "https://image.tmdb.org/t/p/original/prSfAi1xGrhLQNxVSUFh61xQ4Qy.jpg"),
    ("Captain America: The First Avenger", "https://image.tmdb.org/t/p/original/vSNxAJTlD0r02V9sPYpOjqDZXUK.jpg"),
    ("The Avengers", "https://image.tmdb.org/t/p/original/RYMX2wcKCBAr24UyPD7xwmjaTn.jpg"),
    ("Thor: The Dark World", "https://image.tmdb.org/t/p/original/wp6OxE4poJ4G7c0U2ZIXasTSMR7.jpg"),
    ("Captain America: The Winter Soldier", "https://image.tmdb.org/t/p/original/tVFRpFw3xTedgPGqxW0AOI8Qhh0.jpg"),
    ("Guardians of the Galaxy", "https://image.tmdb.org/t/p/original/r7vmZjiyZw9rpJMQJdXpjgiCOk9.jpg"),
    ("Avengers: Age of Ultron", "https://image.tmdb.org/t/p/original/4ssDuvEDkSArWEdyBl2X5EHvYKU.jpg"),
    ("Ant-Man", "https://image.tmdb.org/t/p/original/rS97hUJ1otKTTripGwQ0ujbuIri.jpg"),
    ("Captain America: Civil War", "https://image.tmdb.org/t/p/original/kSBXou5Ac7vEqKd97wotJumyJvU.jpg"),
    ("Doctor Strange", "https://image.tmdb.org/t/p/original/uGBVj3bEbCoZbDjjl9wTxcygko1.jpg"),
    ("Guardians of the Galaxy Vol. 2", "https://image.tmdb.org/t/p/original/y4MBh0EjBlMuOzv9axM4qJlmhzz.jpg"),
    ("Spider-Man: Homecoming", "https://image.tmdb.org/t/p/original/kY2c7wKgOfQjvbqe7yVzLTYkxJO.jpg"),
    ("Thor: Ragnarok", "https://image.tmdb.org/t/p/original/kaIfm5ryEOwYg8mLbq8HkPuM1Fo.jpg"),
    ("Black Panther", "https://image.tmdb.org/t/p/original/uxzzxijgPIY7slzFvMotPv8wjKA.jpg"),
    ("Avengers: Infinity War", "https://image.tmdb.org/t/p/original/7WsyChQLEftFiDOVTGkv3hFpyyt.jpg"),
    ("Ant-Man and the Wasp", "https://image.tmdb.org/t/p/original/eivQmS3wqzqnQWILHLc4FsEfcXP.jpg"),
    ("Captain Marvel", "https://image.tmdb.org/t/p/original/AtsgWhDnHTq68L0lLsUrCnM7TjG.jpg"),
    ("Avengers: Endgame", "https://image.tmdb.org/t/p/original/or06FN3Dka5tukK1e9sl16pB3iy.jpg"),
    ("Spider-Man: Far From Home", "https://image.tmdb.org/t/p/original/4q2NNj4S5dG2RLF9CpXsej7yXl.jpg"),
    ("Black Widow", "https://image.tmdb.org/t/p/original/qAZ0pzat24kLdO3o8ejmbLxyOac.jpg"),
    ("Shang-Chi and the Legend of the Ten Rings", "https://image.tmdb.org/t/p/original/1BIoJGKbXjdFDAqUEiA2VHqkK1Z.jpg"),
    ("Spider-Man: No Way Home", "https://image.tmdb.org/t/p/original/1g0dhYtq4irTY1GPXvft6k4YLjm.jpg"),
    ("Doctor Strange in the Multiverse of Madness", "https://image.tmdb.org/t/p/original/wRnbWt44nKjsFPrqSmwYki5vZtF.jpg"),
    ("Thor: Love and Thunder", "https://image.tmdb.org/t/p/original/pIkRyD18kl4FhoCNQuWxWu5cBLM.jpg"),
    ("Black Panther: Wakanda Forever", "https://image.tmdb.org/t/p/original/sv1xJUazXeYqALzczSZ3O6nkH75.jpg"),
    ("Pulp Fiction", "https://image.tmdb.org/t/p/original/d5iIlFn5s0ImszYzBPb8JPIfbXD.jpg"),
    ("The Shawshank Redemption", "https://image.tmdb.org/t/p/original/q6y0Go1tsGEsmtFryDOJo3dEmqu.jpg"),
    ("2001: A Space Odyssey", "https://image.tmdb.org/t/p/original/ve72VxNqjGM69Uky4WTo7bK6rfq.jpg"),
    ("Psycho", "https://image.tmdb.org/t/p/original/81d8oyEFgj7FlxJqSDXWr8JH8kV.jpg"),
    ("Fight Club", "https://image.tmdb.org/t/p/original/bptfVGEQuv6vDTIMVCHjJ9Dz8PX.jpg"),
    ("Forrest Gump", "https://image.tmdb.org/t/p/original/arw2vcBveWOVZr6pxd9XTd1TdQa.jpg"),
    ("The Silence of the Lambs", "https://image.tmdb.org/t/p/original/rplLJ2hPcOQmkFhTqUte0MkEaO2.jpg"),
    ("Saving Private Ryan", "https://image.tmdb.org/t/p/original/miDoEMlYDJhOCvxlzI0wZqBs9Yt.jpg"),
    ("Jurassic Park", "https://image.tmdb.org/t/p/original/1vZ0qBz0b0f3dQW4lJ3Ejj6LikU.jpg"),
    ("Gladiator", "https://image.tmdb.org/t/p/original/ty8TGRuvJLPUmAR1H1nRIsgwvim.jpg"),
    ("Apocalypse Now", "https://image.tmdb.org/t/p/original/gQB8Y5RCMkv2zwzFHbUJX3kAhvA.jpg"),
    ("Rear Window", "https://image.tmdb.org/t/p/original/qitnZcLP7C9DLRuPpmCjOSO8P08.jpg"),
    ("Star Wars", "https://image.tmdb.org/t/p/original/6FfCtAuVAW8XJjZ7eWeLibRLWTw.jpg")
]

output_dir = "Posters"
os.makedirs(output_dir, exist_ok=True)

def safe_filename(name):
    return re.sub(r'[\\/*?:"<>|]', "", name)

print(f"Pobieranie {len(movies_data)} plakatów...")

for title, url in movies_data:
    try:
        print(f"Pobieranie: {title}", end="\r")
        r = requests.get(url, timeout=30)
        r.raise_for_status()

        filename = safe_filename(title) + ".jpg"
        with open(os.path.join(output_dir, filename), "wb") as f:
            f.write(r.content)

    except Exception as e:
        print(f"\nBłąd przy {title}: {e}")

print("\nGotowe — nazwy plików odpowiadają tytułom filmów")