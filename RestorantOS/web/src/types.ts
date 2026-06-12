export type UserRole = 'Admin' | 'Waiter' | 'Cashier';
export type TableStatus = 'Empty' | 'Occupied' | 'Billed' | 'Paid';
export type SessionStatus = 'Open' | 'Billed' | 'Paid' | 'Cancelled';
export type PaymentMethod = 'Cash' | 'CreditCard' | 'DebitCard' | 'Transfer';
export type OrderItemStatus = 'Pending' | 'Preparing' | 'Served' | 'Cancelled';

export interface AuthUser {
  token: string;
  userId: string;
  fullName: string;
  username: string;
  role: UserRole;
}

export interface TableCard {
  tableId: string;
  tableNumber: number;
  name: string;
  capacity: number;
  section: string;
  status: TableStatus;
  sessionId?: string;
  guestCount: number;
  openedAt?: string;
  totalAmount: number;
  waiterInitials?: string;
}

export interface TableSettings {
  tableId: string;
  tableNumber: number;
  name: string;
  capacity: number;
  section: string;
  isActive: boolean;
  displayOrder: number;
}

export interface MenuItem {
  menuItemId: string;
  categoryId: string;
  name: string;
  description?: string;
  price: number;
  taxRate: number;
  isAvailable: boolean;
  prepTimeMinutes?: number;
}

export interface MenuCategory {
  categoryId: string;
  name: string;
  icon: string;
  items: MenuItem[];
}

export interface OrderItem {
  orderItemId: string;
  menuItemId: string;
  name: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  status: OrderItemStatus;
}

export interface SessionDetail {
  sessionId: string;
  tableId: string;
  tableNumber: number;
  tableName: string;
  openedAt: string;
  guestCount: number;
  status: SessionStatus;
  totalAmount: number;
  discountAmount: number;
  taxAmount: number;
  finalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  orderItems: OrderItem[];
}

export interface TodaySoldItem {
  itemName: string;
  quantity: number;
  revenue: number;
}

export interface Dashboard {
  todayRevenue: number;
  revenueTrendPercent: number;
  openTables: number;
  totalTables: number;
  activeOrdersCount: number;
  averageTicketValue: number;
  todaySessionCount: number;
  todayItemsSold: number;
  todaySoldItems: TodaySoldItem[];
}

export interface DailyRevenueDetail {
  date: string;
  revenue: number;
  sessions: number;
  itemsSold: number;
}

export interface SalesReport {
  from: string;
  to: string;
  totalRevenue: number;
  totalSessions: number;
  totalItemsSold: number;
  averageTicket: number;
  dailyBreakdown: DailyRevenueDetail[];
  topItems: TodaySoldItem[];
}

export interface KanbanOrder {
  orderItemId: string;
  itemName: string;
  quantity: number;
  status: OrderItemStatus;
  tableNumber: number;
  tableName: string;
  section: string;
  createdAt: string;
  rowVersion: string;
}
