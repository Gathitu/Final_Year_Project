import 'dart:io';

import 'package:flutter/material.dart';

import 'screens/menu_screen.dart';
import 'screens/with_arkit_screen.dart';
import 'package:firebase_database/firebase_database.dart';
import 'package:firebase_core/firebase_core.dart';
// import 'firebase_options.dart';

class MyHttpOverrides extends HttpOverrides{
  HttpClient createHttpClient(SecurityContext context){
    return super.createHttpClient(context)
        ..badCertificateCallback = (X509Certificate cert,String host,int port)=> true;
  }
}

void main() async{
  // HttpOverrides.global = new MyHttpOverrides();
  WidgetsFlutterBinding.ensureInitialized();
  await Firebase.initializeApp(
    // options: DefaultFirebaseOptions.currentPlatform,
  );

  runApp(MaterialApp(
    title: 'Named Routes Demo',
    debugShowCheckedModeBanner: false,
    // Start the app with the "/" named route. In this case, the app starts
    // on the FirstScreen widget.
    initialRoute: '/',
    routes: {
      // '/': (context) => MenuScreen(),
      '/': (context) => WithARkitScreen(),
      // '/ar': (context) => WithARkitScreen(),
    },
  ));
}