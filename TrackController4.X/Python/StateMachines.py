

class State:
    def RunFunction(self, argument):
        function = getattr(self, argument, lambda: "function does not exist")
        return function()
    
    def init(self):
        print ("Init called")
        return "ok"
    
    def check_sw_version(self):
        print("Check SW version called")
        return "ok"