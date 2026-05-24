\# 🐢 TurtleGoal - Tortoise Steps

> "Slow and steady wins the race!" 🏁



\*\*TurtleGoal\*\* is a smart, gamified, and social mobile platform designed to help users achieve their goals, developed during an intensive 20-hour hackathon.

The app helps users overcome procrastination and complete complex tasks using "The Tortoise Approach" — breaking down massive goals into small, manageable daily steps using Generative AI, while leveraging crowdsourcing for community support and guidance.



\## ✨ Key Features

\* \*\*🧠 AI Goal Splitter:\*\* The user inputs a main goal, timeframe, and available resources. The app uses a GenAI API to automatically break down the goal into a chronological roadmap of micro-tasks (daily/hourly steps).

\* \*\*📊 The Race Tracker:\*\* A dynamic, gamified tracker. Users check off completed micro-tasks, and a visual progress bar updates to show a tortoise moving closer to the finish line.

\* \*\*💡 Async Tip Engine (Crowdsourcing):\*\* Goals can be set as public. Other users can browse the Community Feed, inspect roadmaps, and leave highly-targeted tips on specific steps. These tips are saved in the cloud and displayed asynchronously to the original user.



\## 🛠 Tech Stack

\* \*\*Frontend:\*\* Native Android development using Xamarin (C#) and XML layouts (Material Design).

\* \*\*Backend \& DB:\*\* Firebase Authentication for user management and Firebase Realtime Database / Firestore for storing goals, steps, and the community tip engine (Big Data).

\* \*\*AI Integration:\*\* GenAI REST API (e.g., OpenAI / Gemini) for natural language processing and roadmap generation.

\* \*\*IDE:\*\* Visual Studio.



\## 📱 Screens Architecture (MVP)

The app is built on a lean, dynamic architecture optimized for rapid hackathon development:

1\. \*\*Splash Screen:\*\* App branding and entry point.

2\. \*\*Auth Screen:\*\* A combined login and registration screen utilizing a `TabLayout`.

3\. \*\*Dashboard:\*\* The main hub displaying the user's "Open Races" (top) and today's "Tortoise Steps" (bottom).

4\. \*\*Create Goal:\*\* An input form to trigger the AI and generate a new roadmap.

5\. \*\*Community Feed:\*\* A social feed displaying public goals from the community.

6\. \*\*Dynamic Roadmap:\*\* A smart roadmap screen. In "Personal Mode", it allows checking off tasks and reading tips. In "Community Mode", it allows viewing another user's plan and asynchronously injecting new tips into their steps.



\---

\*Developed with ❤️ during the 2026 Hackathon\* 🐢🏆

