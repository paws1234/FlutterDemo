import 'package:flutter/material.dart';

void main() {
  runApp(const OrderTrackerApp());
}

/// Root of the Order Tracker demo. The tracker page, the status badge and the
/// live SignalR binding arrive in Phases 2-3; this task fixes the shell and the
/// `lib/` layout of plan §4.1 that they are written into.
class OrderTrackerApp extends StatelessWidget {
  const OrderTrackerApp({super.key});

  @override
  Widget build(BuildContext context) {
    return const MaterialApp(
      title: 'Order Tracker',
      home: Scaffold(body: Center(child: Text('Order Tracker'))),
    );
  }
}
