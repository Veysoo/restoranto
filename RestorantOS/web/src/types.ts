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

export interface MenuItem {
  menuItemId: string;
  categoryId: string;
  name: string;
  description?: string;
  price: number;
  taxRate: number;
  isAvailable: boolean;
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
